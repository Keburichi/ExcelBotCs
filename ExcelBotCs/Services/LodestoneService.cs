using NetStone;

namespace ExcelBotCs.Services;

public class LodestoneService
{
    public LodestoneService()
    {
        
    }

    public async Task InitializeClientAsync()
    {
        try
        {
            var lodestoneClient = await LodestoneClient.GetClientAsync();
            var character = await lodestoneClient.GetCharacter("");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public void Test()
    {
        
    }
}