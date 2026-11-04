using System.Text;

namespace KotShop.WebAdmin.Security;

internal static class MasterBootstrapCommand
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Execute --bootstrap-master em um terminal interativo. A senha será solicitada sem eco.");
            return 1;
        }
        await using var scope = services.CreateAsyncScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstMasterBootstrap>();
        try
        {
            if (await bootstrap.HasMasterAsync())
            {
                Console.WriteLine("Já existe um administrador master. Nenhuma conta foi alterada.");
                return 1;
            }
            Console.WriteLine("Criação do primeiro administrador master do KotShop.");
            Console.WriteLine("Use um e-mail ainda não cadastrado. A conta será criada com e-mail confirmado.");
            Console.Write("E-mail: ");
            var email = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(email)) return 1;
            Console.WriteLine("Senha: mínimo de 12 caracteres, incluindo maiúscula, minúscula, número e símbolo.");
            Console.Write("Senha: ");
            var password = ReadPassword();
            Console.Write("Confirme a senha: ");
            if (password != ReadPassword())
            {
                Console.Error.WriteLine("As senhas não coincidem. Nenhuma conta foi criada.");
                return 1;
            }
            var result = await bootstrap.CreateAsync(email, password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) Console.Error.WriteLine(error.Description);
                return 1;
            }
            Console.WriteLine("Administrador master criado. Entre no WebAdmin com o e-mail e a senha informados.");
            return 0;
        }
        catch (Exception exception)
        {
            // Do not print connection details, credentials or SQL parameter values.
            Console.Error.WriteLine($"Não foi possível concluir a criação ({exception.GetType().Name}). Verifique a conexão e aplique as migrations de Identity antes de tentar novamente.");
            return 1;
        }
    }

    private static string ReadPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return password.ToString(); }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0) password.Length--;
            }
            else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
        }
    }
}
