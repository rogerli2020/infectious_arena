using Unity.NetCode;
using System.Linq;
using Unity.Multiplayer.Playmode;

public class AutoConnectBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {      
        AutoConnectPort = 7979;

        var tags = CurrentPlayer.ReadOnlyTags();

        if (tags.Contains("ClientServer"))
        {
            // Run both client & server in one world (useful for local testing)
            CreateDefaultClientServerWorlds();
            return true;
        }
        if (tags.Contains("Client"))
        {
            // Run client-only
            CreateClientWorld(defaultWorldName);
            return true;
        }
        if (tags.Contains("Server"))
        {
            // Run server-only
            CreateServerWorld(defaultWorldName);
            return true;
        }

        // If no tags match, fall back to default behavior
        return false;
    }
}