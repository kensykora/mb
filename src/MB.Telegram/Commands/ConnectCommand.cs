using System.Threading.Tasks;

public class ConnectCommand
{
    private readonly string userId;

    public ConnectCommand(string userId)
    {
        this.userId = userId;
    }

    public async Task Process()
    {
        
    }
}