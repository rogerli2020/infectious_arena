mental note:
    goal: figure out how to send RPC for 


public partial struct UIToECSBridgeSystem : ISystem
{
public void OnUpdate(ref SystemState state)
{
// Only run if the HUD exists
var hud = ClientHUDManager.Instance;
if (hud == null)
return;

        if (!string.IsNullOrEmpty(hud.message))
        {
            hud.message = string.Empty;
        }
    }
}