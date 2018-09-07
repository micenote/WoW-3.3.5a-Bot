﻿using AmeisenBotCore;
using AmeisenBotData;
using AmeisenBotDB;
using AmeisenBotFSM;
using AmeisenBotFSM.Enums;
using AmeisenBotLogger;
using AmeisenBotUtilities;
using Magic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace AmeisenBotManager
{
    /// <summary>
    /// This Singleton provides an Interface to the bot at a single point
    /// </summary>
    public class BotManager
    {
        private static readonly object padlock = new object();
        private static BotManager instance;
        private readonly string sqlConnectionString =
                "server={0};port={1};database={2};uid={3};password={4};";

        public static BotManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new BotManager();
                    }

                    return instance;
                }
            }
        }

        public List<WowObject> ActiveWoWObjects { get { return AmeisenDataHolder.Instance.ActiveWoWObjects; } }
        private AmeisenClient AmeisenClient { get; set; }
        private AmeisenDBManager AmeisenDBManager { get; set; }
        private AmeisenHook AmeisenHook { get; set; }
        private AmeisenObjectManager AmeisenObjectManager { get; set; }
        private AmeisenSettings AmeisenSettings { get; set; }
        private AmeisenStateMachineManager AmeisenStateMachineManager { get; set; }
        private BlackMagic Blackmagic { get; set; }

        public bool IsAllowedToAssistParty
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToAssistParty; }
            set { AmeisenDataHolder.Instance.IsAllowedToAssistParty = value; }
        }

        public bool IsAllowedToAttack
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToAttack; }
            set { AmeisenDataHolder.Instance.IsAllowedToAttack = value; }
        }

        public bool IsAllowedToBuff
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToBuff; }
            set { AmeisenDataHolder.Instance.IsAllowedToBuff = value; }
        }

        public bool IsAllowedToFollowParty
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToFollowParty; }
            set { AmeisenDataHolder.Instance.IsAllowedToFollowParty = value; }
        }

        public bool IsAllowedToHeal
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToHeal; }
            set { AmeisenDataHolder.Instance.IsAllowedToHeal = value; }
        }

        public bool IsAllowedToTank
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToTank; }
            set { AmeisenDataHolder.Instance.IsAllowedToTank = value; }
        }

        public bool IsAllowedToReleaseSpirit
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToReleaseSpirit; }
            set { AmeisenDataHolder.Instance.IsAllowedToReleaseSpirit = value; }
        }

        public bool IsAllowedToRevive
        {
            get { return AmeisenDataHolder.Instance.IsAllowedToRevive; }
            set { AmeisenDataHolder.Instance.IsAllowedToRevive = value; }
        }

        public bool IsAttached { get; private set; }
        public bool IsHooked { get; private set; }
        public Me Me { get { return AmeisenDataHolder.Instance.Me; } }
        public List<WowExe> RunningWoWs { get { return AmeisenCore.GetRunningWoWs(); } }
        public Settings Settings { get { return AmeisenSettings.Settings; } }
        public Unit Target { get { return AmeisenDataHolder.Instance.Target; } }
        public WowExe WowExe { get; private set; }
        public List<WowObject> WoWObjects { get { return AmeisenObjectManager.GetObjects(); } }
        public Process WowProcess { get; private set; }

        private BotManager()
        {
            IsAttached = false;
            IsHooked = false;

            AmeisenSettings = AmeisenSettings.Instance;
            AmeisenClient = AmeisenClient.Instance;
            AmeisenDBManager = AmeisenDBManager.Instance;
        }

        public int MapID { get { return AmeisenCore.GetMapID(); } }
        public int ZoneID { get { return AmeisenCore.GetZoneID(); } }
        public string LoadedConfigName { get { return AmeisenSettings.loadedconfName; } }

        public List<NetworkBot> NetworkBots
        {
            get
            {
                if (AmeisenClient.IsRegistered)
                {
                    return AmeisenClient.BotList;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsIngame
        {
            get
            {
                return AmeisenCore.CheckWorldLoaded()
                   && !AmeisenCore.CheckLoadingScreen();
            }
        }

        public bool IsRegisteredAtServer { get { return AmeisenClient.IsRegistered; } }

        public void LoadCombatClass(string fileName)
        {
            AmeisenSettings.Settings.combatClassPath = fileName;
            AmeisenSettings.SaveToFile(AmeisenSettings.loadedconfName);

            //TODO: replace AmeisenCombatManager.ReloadCombatClass();
        }

        public void LoadSettingsFromFile(string filename)
        {
            AmeisenSettings.LoadFromFile(filename);
        }

        public void SaveSettingsToFile(string filename)
        {
            AmeisenSettings.SaveToFile(filename);
        }

        public void StartBot(WowExe wowExe)
        {
            WowExe = wowExe;

            // Load Settings
            AmeisenSettings.LoadFromFile(wowExe.characterName);

            // Connect to DB
            if (AmeisenSettings.Settings.databaseAutoConnect)
            {
                AmeisenDBManager.ConnectToMySQL(
                string.Format(sqlConnectionString,
                    AmeisenSettings.Settings.databaseIP,
                    AmeisenSettings.Settings.databasePort,
                    AmeisenSettings.Settings.databaseName,
                    AmeisenSettings.Settings.databaseUsername,
                    AmeisenSettings.Settings.databasePasswort)
                );
            }

            // Attach to Proccess
            Blackmagic = new BlackMagic(wowExe.process.Id);
            IsAttached = Blackmagic.IsProcessOpen;
            // TODO: make this better
            AmeisenCore.BlackMagic = Blackmagic;

            // Hook EndScene
            AmeisenHook = AmeisenHook.Instance;
            IsHooked = AmeisenHook.isHooked;

            // Start our object updates
            AmeisenObjectManager = AmeisenObjectManager.Instance;
            AmeisenObjectManager.Start();

            // Start the StateMachine
            AmeisenStateMachineManager = new AmeisenStateMachineManager();
            AmeisenStateMachineManager.StateMachine.PushAction(BotState.Idle);
            AmeisenStateMachineManager.StateMachine.PushAction(BotState.Follow);
            AmeisenStateMachineManager.Start();

            // Connect to Server
            if (Settings.serverAutoConnect)
            {
                AmeisenClient.Register(
                    Me,
                    IPAddress.Parse(AmeisenSettings.Settings.ameisenServerIP),
                    AmeisenSettings.Settings.ameisenServerPort);
            }
        }

        public void StopBot()
        {
            // Disconnect from Server
            if (AmeisenClient.IsRegistered)
            {
                AmeisenClient.Unregister();
            }

            // Stop object updates
            AmeisenObjectManager.Stop();

            // Stop the statemachine
            AmeisenStateMachineManager.Stop();

            // Unhook the EndScene
            AmeisenHook.DisposeHooking();

            // Detach BlackMagic, causing weird crash right now...
            //Blackmagic.Close();

            // Stop logging
            AmeisenLogger.Instance.StopLogging();

            //Close SQL Connection
            AmeisenDBManager.Instance.Disconnect();
        }
    }
}