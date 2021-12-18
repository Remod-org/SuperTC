#region License (GPL v3)
/*
    DESCRIPTION
    Copyright (c) 2021 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v3)
using System;
using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("SuperTC", "RFC1920", "1.0.2")]
    [Description("SuperTC looks tough and protects tough")]
    internal class SuperTC : RustPlugin
    {
        [PluginReference]
        private readonly Plugin SignArtist;

        private ConfigData configData;
        public static SuperTC Instance;
        private const string permUse = "supertc.use";
        private bool startup;

        public List<uint> tcs = new List<uint>();
        private readonly List<string> orDefault = new List<string>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "off", "OFF" },
                { "on", "ON" },
                { "notauthorized", "You don't have permission to do that !!" },
                { "enabled", "SuperTC enabled" },
                { "disabled", "SuperTC disabled" }
            }, this);
        }

        private void DoLog(string message)
        {
            if (configData.debug) Puts(message);
        }

        private void Init()
        {
            LoadConfigValues();
            AddCovalenceCommand("stc", "EnableDisable");
            permission.RegisterPermission(permUse, this);
        }

        private void Unload()
        {
            foreach (ExtendedTC xtc in UnityEngine.Object.FindObjectsOfType<ExtendedTC>())
            {
                UnityEngine.Object.Destroy(xtc);
            }
        }

        private void OnServerInitialized()
        {
            Instance = this;
            LoadData();

            foreach (ulong tcid in tcs)
            {
                BaseNetworkable tc = BaseNetworkable.serverEntities.Find(uint.Parse(tcid.ToString()));
                if (tc != null)
                {
                    ExtendedTC xtc = tc.gameObject.GetComponent<ExtendedTC>();
                    if (xtc != null)
                    {
                        DoLog("Destroying old supertc");
                        UnityEngine.Object.Destroy(xtc);
                    }
                    DoLog("Adding new supertc");
                    tc.gameObject.AddComponent<ExtendedTC>();
                }
            }

            startup = true;
        }

        private void LoadData()
        {
            tcs = Interface.Oxide.DataFileSystem.ReadObject<List<uint>>(Name + "/tcs") ?? new List<uint>();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/tcs", tcs);
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            string majority = hitinfo.damageTypes.GetMajorityDamageType().ToString();
            if (majority == "Decay") return null;

            if (entity?.GetComponent<ExtendedTC>() || entity?.GetComponentInParent<ExtendedTC>())
            {
                DoLog($"This is a SuperTC component!: {entity.ShortPrefabName}");
                return true;
            }

            BuildingPrivlidge bp = entity.GetBuildingPrivilege();
            if (bp?.GetComponent<ExtendedTC>())
            {
                DoLog($"Entity protected by SuperTC!: {entity.ShortPrefabName}");
                return true;
            }

            return null;
        }

        private void OnEntitySpawned(BuildingPrivlidge tc)
        {
            if (!startup) return;
            if (configData.allTCs)
            {
                ExtendedTC obj = tc.gameObject.AddComponent<ExtendedTC>();
                return;
            }

            DoLog("Checking ownerID for TC");
            string ownerid = tc?.OwnerID.ToString();
            BasePlayer player = BasePlayer.FindByID(tc.OwnerID);

            if (player.IsAdmin && configData.adminTCs)
            {
                ExtendedTC obj = tc.gameObject.AddComponent<ExtendedTC>();
                return;
            }

            if (!permission.UserHasPermission(ownerid, permUse) && configData.requirePermission)
            {
                Message(player?.IPlayer, "notauthorized");
                return;
            }

            if (configData.defaultEnabled && orDefault.Contains(ownerid))
            {
                DoLog("Plugin enabled by default, but player-disabled");
                return;
            }
            else if (!configData.defaultEnabled && !orDefault.Contains(ownerid))
            {
                DoLog("Plugin disabled by default, and not player-enabled");
                return;
            }
            tcs.Add(tc.net.ID);
            SaveData();

            tc.gameObject.AddComponent<ExtendedTC>();
        }

        [Command("stc"), Permission(permUse)]
        private void EnableDisable(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.HasPermission(permUse) && configData.requirePermission) { Message(iplayer, "notauthorized"); return; }

            bool en = configData.defaultEnabled;
            if (orDefault.Contains(iplayer.Id))
            {
                orDefault.Remove(iplayer.Id);
            }
            else
            {
                orDefault.Add(iplayer.Id);
                en = !en;
            }
            switch (en)
            {
                case true:
                    Message(iplayer, "enabled");
                    break;
                case false:
                    Message(iplayer, "disabled");
                    break;
            }
        }

        public class ExtendedTC : MonoBehaviour
        {
            public BuildingPrivlidge tc;
            public BaseEntity entity;
            public BaseEntity door1;
            public BaseEntity door2;
            public BaseEntity door3;
            public BaseEntity door4;
            public BaseEntity sign;
            public BaseEntity barry;

            public const string prefabdoor = "assets/prefabs/building/door.hinged/door.hinged.toptier.prefab";
            public const string prefabarry = "assets/prefabs/deployable/barricades/barricade.stone.prefab";
            public const string prefabsign = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
            public const string signurl = "https://i.imgur.com/yvnxmfX.png";

            public void Awake()
            {
                tc = GetComponent<BuildingPrivlidge>();
                entity = tc as BaseEntity;

                if (entity != null)
                {
                    TCPlating();
                }
            }

            public void TCPlating()
            {
                //front
                door1 = SpawnPart(prefabdoor, door1, false, 0, 90, 0, 0f, -0.1f, 0.43f, entity, 0);
                door1.SetFlag(BaseEntity.Flags.Busy, true, true);
                door1.SetFlag(BaseEntity.Flags.Locked, true);

                //rear
                door2 = SpawnPart(prefabdoor, door2, false, 0, 270, 0, 0f, -0.1f, -0.43f, entity, 0);
                door2.SetFlag(BaseEntity.Flags.Busy, true, true);
                door2.SetFlag(BaseEntity.Flags.Locked, true);

                //left
                door3 = SpawnPart(prefabdoor, door3, false, 0, 0, 0, 0.55f, -0.1f, 0f, entity, 0);
                door3.SetFlag(BaseEntity.Flags.Busy, true, true);
                door3.SetFlag(BaseEntity.Flags.Locked, true);

                //right
                door4 = SpawnPart(prefabdoor, door4, false, 0, 180, 0, -0.55f, -0.1f, 0f, entity, 0);
                door4.SetFlag(BaseEntity.Flags.Busy, true, true);
                door4.SetFlag(BaseEntity.Flags.Locked, true);

                //barry
                barry = SpawnPart(prefabarry, barry, false, 0, 0, 0, 0f, 1.2f, 0f, entity, 0);
                barry.SetFlag(BaseEntity.Flags.Busy, true, true);
                barry.SetFlag(BaseEntity.Flags.Locked, true);

                if (Instance.SignArtist)
                {
                    sign = SpawnPart(prefabsign, sign, false, 0, 0, 0, 0f, 1.5f, 0.58f, entity, 0);
                    Instance.SignArtist?.Call("API_SkinSign", null, sign, signurl, true);
                    sign.SetFlag(BaseEntity.Flags.Busy, true, true);
                    sign.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }

            private BaseEntity SpawnPart(string prefab, BaseEntity entitypart, bool setactive, int eulangx, int eulangy, int eulangz, float locposx, float locposy, float locposz, BaseEntity parent, ulong skinid)
            {
                if (Instance.configData.debug)
                {
                    Interface.Oxide.LogInfo($"SpawnPart: {prefab}, active:{setactive.ToString()}, angles:({eulangx.ToString()}, {eulangy.ToString()}, {eulangz.ToString()}), position:({locposx.ToString()}, {locposy.ToString()}, {locposz.ToString()}), parent:{parent.ShortPrefabName} skinid:{skinid.ToString()}");
                }
                entitypart = GameManager.server.CreateEntity(prefab, tc.transform.position, tc.transform.rotation, setactive);
                entitypart.transform.localEulerAngles = new Vector3(eulangx, eulangy, eulangz);
                entitypart.transform.localPosition = new Vector3(locposx, locposy, locposz);

                entitypart.SetParent(parent, 0);
                entitypart.skinID = Convert.ToUInt64(skinid);
                entitypart?.Spawn();
                SpawnRefresh(entitypart);
                return entitypart;
            }

            private void SpawnRefresh(BaseEntity entity)
            {
                StabilityEntity hasstab = entity.GetComponent<StabilityEntity>();
                if (hasstab != null)
                {
                    hasstab.grounded = true;
                }
                BaseMountable hasmount = entity.GetComponent<BaseMountable>();
                if (hasmount != null)
                {
                    hasmount.isMobile = true;
                }
                //Rigidbody hasrigid = entity.GetComponent<Rigidbody>();
                //if (hasrigid != null)
                //{
                //    hasrigid.isKinematic = true;
                //}
            }

            public void OnDestroy()
            {
                door1.Kill();
                door2.Kill();
                door3.Kill();
                door4.Kill();
                barry.Kill();
            }
        }

        private class ConfigData
        {
            public bool allTCs;
            public bool adminTCs;
            public bool defaultEnabled;
            public bool requirePermission;
            public bool debug;
            public VersionNumber Version;
        }

        private void LoadConfigValues()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData()
            {
                allTCs = false,
                adminTCs = false,
                defaultEnabled = true,
                requirePermission = true,
                debug = false,
                Version = Version
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
    }
}
