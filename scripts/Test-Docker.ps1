#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true
Add-Type -AssemblyName System.Net.Http

function Assert-True($Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-Sql([string] $Sql) {
    $output = $Sql | docker compose exec -T postgres psql -U kotshop -d kotshop -v ON_ERROR_STOP=1 -tA
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao executar SQL de verificação.' }
    return $output
}

function Get-FormToken([string] $Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    Assert-True $match.Success 'Token antiforgery não encontrado.'
    return [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

function Send-Image([string] $Uri, $Session, [string] $FilePath, [string] $Token) {
    # HttpClient supports multipart uploads in both Windows PowerShell 5.1 and PowerShell 7.
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.CookieContainer = $Session.Cookies
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $response = $null
    try {
        $form.Add([System.Net.Http.ByteArrayContent]::new([IO.File]::ReadAllBytes($FilePath)), 'arquivo', 'teste.png')
        if ($Token) {
            $form.Add([System.Net.Http.StringContent]::new($Token), '__RequestVerificationToken')
        }
        $response = $client.PostAsync($Uri, $form).GetAwaiter().GetResult()
        return [pscustomobject] @{
            StatusCode = [int] $response.StatusCode
            Content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        }
    }
    finally {
        if ($response) { $response.Dispose() }
        $form.Dispose()
        $client.Dispose()
    }
}

$baseUrl = 'http://localhost:8080'
$email = "docker-smoke-$([guid]::NewGuid().ToString('N'))@example.invalid"
$password = "Smoke!9$([guid]::NewGuid().ToString('N'))"
$imageFile = New-TemporaryFile
$productId = $null
$objectKey = $null

Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $health = Invoke-RestMethod "$baseUrl/health"
    Assert-True ($health.status -eq 'ok') 'Health check falhou.'
    $catalogPage = Invoke-WebRequest "$baseUrl/"
    Assert-True ($catalogPage.Content.Contains('Smartphone')) 'Produtos iniciais não foram exibidos.'

    $register = Invoke-WebRequest "$baseUrl/Account/Register" -SessionVariable session
    $null = Invoke-WebRequest "$baseUrl/Account/Register" -WebSession $session -Method Post -Body @{
        _handler = 'register'
        __RequestVerificationToken = Get-FormToken $register.Content
        'Input.Email' = $email
        'Input.Password' = $password
        'Input.ConfirmPassword' = $password
    }

    # This account and product exist only for this test; no existing users are modified.
    $null = Invoke-Sql @"
UPDATE "AspNetUsers" SET "EmailConfirmed" = true WHERE "Email" = '$email';
INSERT INTO "AspNetUserClaims" ("UserId", "ClaimType", "ClaimValue")
SELECT "Id", 'permissao', 'catalogo:escrever' FROM "AspNetUsers" WHERE "Email" = '$email';
"@
    $productId = Invoke-Sql @'
INSERT INTO "Produtos" ("Nome", "Preco", "Descricao", "CategoriaId")
VALUES ('Teste Docker', 1.00, 'Produto temporário de verificação', 1) RETURNING "Id";
'@
    $productId = @($productId | Where-Object { $_ -match '^\d+$' })[0]
    Assert-True ($null -ne $productId) 'Produto temporário não foi criado.'

    $login = Invoke-WebRequest "$baseUrl/Account/Login" -WebSession $session
    $null = Invoke-WebRequest "$baseUrl/Account/Login" -WebSession $session -Method Post -Body @{
        _handler = 'login'
        __RequestVerificationToken = Get-FormToken $login.Content
        'Input.Email' = $email
        'Input.Password' = $password
    }
    $manage = Invoke-WebRequest "$baseUrl/Account/Manage" -WebSession $session
    $token = Get-FormToken $manage.Content

    $anonymousHandler = [System.Net.Http.HttpClientHandler]::new()
    $anonymousHandler.AllowAutoRedirect = $false
    $anonymousClient = [System.Net.Http.HttpClient]::new($anonymousHandler)
    try {
        $anonymousForm = [System.Net.Http.MultipartFormDataContent]::new()
        $anonymousForm.Add([System.Net.Http.ByteArrayContent]::new([byte[]] @(1, 2, 3)), 'arquivo', 'teste.png')
        $anonymous = $anonymousClient.PostAsync("$baseUrl/api/produtos/$productId/imagem", $anonymousForm).GetAwaiter().GetResult()
        Assert-True ([int] $anonymous.StatusCode -in 401, 302) "Upload anônimo deveria exigir login; recebido $([int] $anonymous.StatusCode)."
    }
    finally { $anonymousClient.Dispose() }

    [IO.File]::WriteAllBytes($imageFile.FullName, [byte[]] @(1, 2, 3))
    $invalidImage = Send-Image "$baseUrl/api/produtos/$productId/imagem" $session $imageFile.FullName $token
    Assert-True ($invalidImage.StatusCode -eq 400) 'Arquivo inválido deveria ser rejeitado.'

    [IO.File]::WriteAllBytes($imageFile.FullName, [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII='))
    $missingToken = Send-Image "$baseUrl/api/produtos/$productId/imagem" $session $imageFile.FullName
    Assert-True ($missingToken.StatusCode -eq 400) 'Upload sem token antiforgery deveria ser rejeitado.'

    $upload = Send-Image "$baseUrl/api/produtos/$productId/imagem" $session $imageFile.FullName $token
    Assert-True ($upload.StatusCode -eq 200) "Upload falhou: HTTP $($upload.StatusCode). $($upload.Content)"
    $result = $upload.Content | ConvertFrom-Json
    Assert-True ($result.urlImagem.StartsWith('http://localhost:4566/kotshop-arquivos/produtos/')) 'URL pública incorreta.'
    $objectKey = ([uri] $result.urlImagem).AbsolutePath.Replace('/kotshop-arquivos/', '')
    $image = Invoke-WebRequest $result.urlImagem
    Assert-True ($image.StatusCode -eq 200) 'Objeto S3 não está acessível.'
    Assert-True ($image.Headers.'Content-Type' -contains 'image/png') 'Content-Type S3 incorreto.'
    $savedUrl = Invoke-Sql "SELECT ""UrlImagem"" FROM ""Produtos"" WHERE ""Id"" = $productId;"
    Assert-True ($savedUrl -eq $result.urlImagem) 'URL não persistida no PostgreSQL.'
    $catalogPage = Invoke-WebRequest "$baseUrl/"
    Assert-True ($catalogPage.Content.Contains($result.urlImagem)) 'Imagem não exibida na vitrine.'

    Write-Output 'OK: health, catálogo, Identity, autorização, antiforgery, upload S3, URL persistida e imagem na vitrine.'
}
finally {
    try {
        if ($objectKey) {
            docker compose exec -T localstack awslocal s3api delete-object --bucket kotshop-arquivos --key $objectKey | Out-Null
        }
        if ($productId) { $null = Invoke-Sql "DELETE FROM ""Produtos"" WHERE ""Id"" = $productId;" }
        $null = Invoke-Sql "DELETE FROM ""AspNetUsers"" WHERE ""Email"" = '$email';"
    }
    finally {
        Remove-Item -LiteralPath $imageFile.FullName -ErrorAction SilentlyContinue
        Pop-Location
    }
}
