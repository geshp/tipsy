using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tipsy.Windows;
using Tipsy;
using System.Threading.Tasks.Dataflow;
using System.ComponentModel.Design;

namespace Tipsy;

public sealed class Plugin : IDalamudPlugin
{
    //ROADMAP
    //1 - **DONE** Chat log manipulation (convert the names in the item links for medicine into alcoholic drinks
    //2 - Update hover-over descriptions of alcoholic drinks
    //3 - Update item icon imagery
    //4 - Add Tipsy and Drunk debuffs
    //5 - Integrate said debuffs into Moodles
    //6 - Random emote firing while tipsy/drunk
    //7 - Visual effects (low aperature visuals, trouble walking/running straight)



    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;

    private const string CommandName = "/tipsy";

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("Tipsy");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private bool bProcessing = false;
    private Dalamud.Game.Text.SeStringHandling.SeStringBuilder sb = new Dalamud.Game.Text.SeStringHandling.SeStringBuilder();

    public Plugin() {

        //Reload any previous configuration
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        //Leaving this here to remember how to instantiate an image
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        //Create both windows, which we may or may not use
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        //Intercept chat
        Plugin.Chat.ChatMessage += OnChatMessage;

        //CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
        //    HelpMessage = "A useful message to display in /xlhelp"
        //});

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        //Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    private void OnChatMessage(XivChatType type, int _, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (isHandled) return;

        //FF14 uses a custom string format to add links that's actually an array
        //of data types.  Fortunately, we only need to tweak with "ItemPayLoad" types.
        sb = new SeStringBuilder();
        int lap = -1; UInt32 itm = 0; string proc = ""; string itmname = ""; 

        //Only uncomment for debugging purposes
        //sb.AddText("[Tipsy] ");

        do
        {
            lap++;
            
            //Status messages sometimes skip a line
            if (message.Payloads.Count == 0) continue;

            //Convert the medicine names to the alcoholic names
            if (message.Payloads[lap].GetType().Name == "ItemPayload")
            {
                proc = message.Payloads[lap].ToString();
                proc = proc.Substring(15);
                proc = proc.Substring(0, proc.IndexOf(","));
                itm = Convert.ToUInt32(proc);
                //TODO: Make an easier alias table
                if (itm == 6141) { itmname = "Green Carbuncle Shot"; }
                ;
                if (itmname == "")
                {
                    sb.AddItemLink(Convert.ToUInt32(proc), false);
                }
                else
                {
                    sb.AddItemLink(Convert.ToUInt32(proc), false, itmname);
                }
                //For some reason, it doubles the item name in chat, so we'll just skip it
                lap = lap + 6;
            }
            else
            {
                //The only type of these we actually need to intercept is ItemPayload but
                //we have to process them all to keep them from messing up unrelated posts
                sb.Add(message.Payloads[lap]);
            }
        } while  (lap < message.Payloads.Count - 1);
        message = sb.BuiltString;
        sb = null;
    }
    public List<Payload> Payloads { get; }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        Plugin.Chat.ChatMessage -= OnChatMessage;
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) {
        //Chat.Print("Tipsy is loaded and now so are you!");

        // in response to the slash command, just toggle the display status of our main ui
        //ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
