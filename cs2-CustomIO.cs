using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Memory;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomIO;

[PluginMetadata(Id = "CS2 CustomIO For SW2", Version = "1.1", Name = "CustomIO SW2", Author = "DarkerZ & LynchMus", Description = "Fixes missing keyvalues from CSS/CS:GO", Website = "https://github.com/himenekocn/CS2-CustomIO-For-SW2")]
public partial class CustomIO(ISwiftlyCore core) : BasePlugin(core)
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CEntityIdentity_SetEntityName_Delegate(nint a1, string a2);
    private static IUnmanagedFunction<CEntityIdentity_SetEntityName_Delegate>? CEntityIdentity_SetEntityName_Func;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CBaseEntity_SetGravityScale_Delegate(nint a1, float a2);
    private static IUnmanagedFunction<CBaseEntity_SetGravityScale_Delegate>? CBaseEntity_SetGravityScale_Func;

    private IConVar<bool>? sw_iodebug;

    public override void Load(bool hotReload)
    {
        sw_iodebug = Core.ConVar.CreateOrFind("sw_iodebug", "Enable IO Debug", false);

        CEntityIdentity_SetEntityName_Func = Core.Memory.GetUnmanagedFunctionByAddress<CEntityIdentity_SetEntityName_Delegate>(Core.GameData.GetSignature("CEntityInstance::SetEntityName"));
        CBaseEntity_SetGravityScale_Func = Core.Memory.GetUnmanagedFunctionByAddress<CBaseEntity_SetGravityScale_Delegate>(Core.GameData.GetSignature("CBaseEntity::SetGravityScale"));

        Core.Event.OnEntityIdentityAcceptInputHook += OnEntityIdentityAcceptInputHook;
    }

    public override void Unload()
    {
        Core.Event.OnEntityIdentityAcceptInputHook -= OnEntityIdentityAcceptInputHook;
    }

    public void OnEntityIdentityAcceptInputHook(IOnEntityIdentityAcceptInputHookEvent @event)
    {
        var input = @event.InputName;
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        var ceid = @event.Identity;

        if (ceid == null || !ceid.IsValid)
        {
            return;
        }

        var cei = ceid.EntityInstance;

        if (cei == null || !cei.IsValid)
        {
            return;
        }

        var activator = @event.Activator;
        var value = TryToString(@event.VariantValue);

        try
        {
            if (input.StartsWith("keyvalue", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if(sw_iodebug?.Value == true)
                        Core.Logger.LogInformation($"[CustomIO]: {input} {value}");
                    string[] keyvalue = value.Split([' ']);
                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[0]))
                    {
                        switch (keyvalue[0].ToLower())
                        {
                            case "targetname":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        CEntityIdentity_SetEntityName_Func?.Call(ceid.Address, keyvalue[1]);
                                    }
                                }
                                break;
                            case "origin":
                                {
                                    if (keyvalue.Length >= 4 && !string.IsNullOrEmpty(keyvalue[1]) && !string.IsNullOrEmpty(keyvalue[2]) && !string.IsNullOrEmpty(keyvalue[3]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float x) && float.TryParse(keyvalue[2], out float y) && float.TryParse(keyvalue[3], out float z))
                                        {
                                            x = Math.Clamp(x, -16384.0f, 16384.0f);
                                            y = Math.Clamp(y, -16384.0f, 16384.0f);
                                            z = Math.Clamp(z, -16384.0f, 16384.0f);
                                            cei.As<CBaseEntity>().Teleport(new(x, y, z), null, null);
                                        }
                                    }
                                }
                                break;
                            case "angles":
                                {
                                    if (keyvalue.Length >= 4 && !string.IsNullOrEmpty(keyvalue[1]) && !string.IsNullOrEmpty(keyvalue[2]) && !string.IsNullOrEmpty(keyvalue[3]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float x) && float.TryParse(keyvalue[2], out float y) && float.TryParse(keyvalue[3], out float z))
                                        {
                                            x = Math.Clamp(x, -360.0f, 360.0f);
                                            y = Math.Clamp(y, -360.0f, 360.0f);
                                            z = Math.Clamp(z, -360.0f, 360.0f);
                                            cei.As<CBaseEntity>().Teleport(null, new(x, y, z), null);
                                        }
                                    }
                                }
                                break;
                            case "max_health":
                                {
                                    int iMaxHealth = 100;
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && int.TryParse(keyvalue[1], out iMaxHealth))
                                    {
                                        var entity = cei.As<CBaseEntity>();
                                        if (entity != null && entity.IsValid)
                                        {
                                            entity.MaxHealth = iMaxHealth;
                                            entity.MaxHealthUpdated();
                                        }
                                    }
                                }
                                break;
                            case "health":
                                {
                                    int iHealth = 100;
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && int.TryParse(keyvalue[1], out iHealth))
                                    {
                                        var entity = cei.As<CBaseEntity>();
                                        if (entity != null && entity.IsValid)
                                        {
                                            entity.Health = iHealth;
                                            entity.HealthUpdated();
                                        }
                                    }
                                }
                                break;
                            case "movetype":
                                {
                                    var player = EntityToPlayer(cei.As<CBaseEntity>());
                                    if (player != null && player.IsValid && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        if (byte.TryParse(keyvalue[1], out byte iMovetype))
                                        {
                                            iMovetype = Math.Clamp(iMovetype, (byte)MoveType_t.MOVETYPE_NONE, (byte)MoveType_t.MOVETYPE_LAST);
                                            player.PlayerPawn.Value.MoveType = (MoveType_t)iMovetype;
                                            player.PlayerPawn.Value.ActualMoveType = (MoveType_t)iMovetype;
                                            player.PlayerPawn.Value.MoveTypeUpdated();
                                        }
                                    }
                                }
                                break;
                            case "entitytemplate":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && string.Equals(cei.DesignerName, "env_entity_maker"))
                                    {
                                        cei.As<CEnvEntityMaker>()?.Template = keyvalue[1];
                                    }
                                }
                                break;
                            case "basevelocity":
                                {
                                    if (keyvalue.Length >= 4 && !string.IsNullOrEmpty(keyvalue[1]) && !string.IsNullOrEmpty(keyvalue[2]) && !string.IsNullOrEmpty(keyvalue[3]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float x) && float.TryParse(keyvalue[2], out float y) && float.TryParse(keyvalue[3], out float z))
                                        {
                                            x = Math.Clamp(x, -4096.0f, 4096.0f);
                                            y = Math.Clamp(y, -4096.0f, 4096.0f);
                                            z = Math.Clamp(z, -4096.0f, 4096.0f);
                                            var entity = cei.As<CBaseEntity>();
                                            if (entity != null && entity.IsValid)
                                            {
                                                entity.BaseVelocity.X = x;
                                                entity.BaseVelocity.Y = y;
                                                entity.BaseVelocity.Z = z;
                                                entity.BaseVelocityUpdated();
                                            }
                                        }
                                    }
                                }
                                break;
                            case "absvelocity":
                                {
                                    if (keyvalue.Length >= 4 && !string.IsNullOrEmpty(keyvalue[1]) && !string.IsNullOrEmpty(keyvalue[2]) && !string.IsNullOrEmpty(keyvalue[3]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float x) && float.TryParse(keyvalue[2], out float y) && float.TryParse(keyvalue[3], out float z))
                                        {
                                            x = Math.Clamp(x, -4096.0f, 4096.0f);
                                            y = Math.Clamp(y, -4096.0f, 4096.0f);
                                            z = Math.Clamp(z, -4096.0f, 4096.0f);
                                            var entity = cei.As<CBaseEntity>();
                                            if (entity != null && entity.IsValid)
                                            {
                                                entity.Teleport(null, null, new(x, y, z));
                                            }
                                        }
                                    }
                                }
                                break;
                            case "target":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && FindEntityByName(keyvalue[1]) is CEntityInstance gettarget && gettarget.IsValid)
                                    {
                                        var entity = cei.As<CBaseEntity>();
                                        if (entity != null && entity.IsValid)
                                        {
                                            entity.Target = gettarget.Entity!.Name;
                                        }
                                    }
                                }
                                break;
                            case "filtername":
                                {
                                    if (cei.DesignerName.StartsWith("trigger_") && keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && FindEntityByName(keyvalue[1]) is CEntityInstance gettarget && gettarget.IsValid && gettarget.DesignerName.StartsWith("filter_"))
                                    {
                                        var trigger = cei.As<CBaseTrigger>();
                                        trigger?.FilterName = gettarget.Entity!.Name;
                                        trigger?.Filter.Raw = gettarget.Entity!.EntityHandle.Raw;
                                    }
                                }
                                break;
                            case "force":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && string.Equals(cei.DesignerName, "phys_thruster"))
                                    {
                                        if (float.TryParse(keyvalue[1], out float fForce))
                                        {
                                            cei.As<CPhysThruster>()?.Force = fForce;
                                        }
                                    }
                                }
                                break;
                            case "gravity":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float fGravity))
                                        {
                                            var entity = cei.As<CBaseEntity>();
                                            if (entity != null && entity.IsValid)
                                            {
                                                CBaseEntity_SetGravityScale_Func?.Call(entity.Address, fGravity);
                                            }
                                        }
                                    }
                                }
                                break;
                            case "timescale":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float fTimeScale))
                                        {
                                            var entity = cei.As<CBaseEntity>();
                                            if (entity != null && entity.IsValid)
                                            {
                                                entity.TimeScale = fTimeScale;
                                                entity.TimeScaleUpdated();
                                            }
                                        }
                                    }
                                }
                                break;
                            case "friction":
                                {
                                    if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        if (float.TryParse(keyvalue[1], out float fFriction))
                                        {
                                            var entity = cei.As<CBaseEntity>();
                                            if (entity != null && entity.IsValid)
                                            {
                                                entity.Friction = fFriction;
                                                entity.FrictionUpdated();
                                            }
                                        }
                                    }
                                }
                                break;
                            case "speed":
                                {
                                    var player = EntityToPlayer(cei.As<CBaseEntity>());
                                    if (player != null && player.IsValid && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.MovementServices != null && keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        float fSpeed = 0.001f;
                                        if (float.TryParse(keyvalue[1], out fSpeed))
                                        {
                                            if (fSpeed <= 0.0f) fSpeed = 0.001f;
                                            player.PlayerPawn.Value.MovementServices.Maxspeed = 260.0f * fSpeed;
                                            player.PlayerPawn.Value.MovementServices.MaxspeedUpdated();
                                            player.PlayerPawn.Value.VelocityModifier = fSpeed;
                                            player.PlayerPawn.Value.VelocityModifierUpdated();
                                        }
                                    }
                                }
                                break;
                            case "runspeed":
                                {
                                    var player = EntityToPlayer(cei.As<CBaseEntity>());
                                    if (player != null && player.IsValid && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]))
                                    {
                                        float fRunSpeed = 0.001f;
                                        if (float.TryParse(keyvalue[1], out fRunSpeed))
                                        {
                                            if (fRunSpeed <= 0.0f) fRunSpeed = 0.001f;
                                            player.PlayerPawn.Value.VelocityModifier = fRunSpeed;
                                            player.PlayerPawn.Value.VelocityModifierUpdated();
                                        }
                                    }
                                }
                                break;
                            case "damage":
                                {
                                    if (string.Equals(cei.DesignerName, "point_hurt"))
                                    {
                                        if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && int.TryParse(keyvalue[1], out int iDamage))
                                        {
                                            cei.As<CPointHurt>()?.Damage = iDamage;
                                        }
                                    }
                                }
                                break;
                            case "damagetype":
                                {
                                    if (string.Equals(cei.DesignerName, "point_hurt"))
                                    {
                                        if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && int.TryParse(keyvalue[1], out int iBitsDamageType))
                                        {
                                            cei.As<CPointHurt>()?.BitsDamageType = (DamageTypes_t)iBitsDamageType;
                                        }
                                    }
                                }
                                break;
                            case "damageradius":
                                {
                                    if (string.Equals(cei.DesignerName, "point_hurt"))
                                    {
                                        if (keyvalue.Length >= 2 && !string.IsNullOrEmpty(keyvalue[1]) && int.TryParse(keyvalue[1], out int iDamageRadius))
                                        {
                                            cei.As<CPointHurt>()?.Radius = iDamageRadius;
                                        }
                                    }
                                }
                                break;
                            case "case":
                                {
                                    if (string.Equals(cei.DesignerName, "logic_case"))
                                    {
                                        if (keyvalue.Length >= 3 && !string.IsNullOrEmpty(keyvalue[1]) && !string.IsNullOrEmpty(keyvalue[2]) && int.TryParse(keyvalue[1], out int iCase))
                                        {
                                            string sArgs = keyvalue[2];
                                            for (int i = 3; i < keyvalue.Length; i++)
                                            {
                                                if (!string.IsNullOrEmpty(keyvalue[i]))
                                                    sArgs += " " + keyvalue[i];
                                            }
                                            if (iCase >= 1 && iCase <= 32)
                                            {
                                                cei.As<CLogicCase>()?.Case[iCase - 1] = sArgs;
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            else if (Equals(input.ToLower(), "addscore"))
            {
                var player = EntityToPlayer(activator);
                if (player != null && player.IsValid && int.TryParse(value, out int iscore))
                {
                    player.Score += iscore;
                    player.ScoreUpdated();
                }
            }
            else if (Equals(input.ToLower(), "setmessage") && string.Equals(cei.DesignerName, "env_hudhint"))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    cei.As<CEnvHudHint>()?.Message = value;
                }
            }
            else if (Equals(input.ToLower(), "setmodel"))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var player = EntityToPlayer(activator);
                    if (player != null && player.IsValid)
                    {
                        var pawn = player.PlayerPawn.Value;
                        if (pawn != null && pawn.IsValid)
                        {
                            pawn.SetModel(value);
                            if (pawn.ActualMoveType > MoveType_t.MOVETYPE_OBSOLETE)
                            {
                                var originalVelocity = pawn.AbsVelocity;
                                pawn.Teleport(null, null, Vector.Zero);
                                pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
                                var cHandle = pawn.Entity!.EntityHandle;

                                Core.Scheduler.DelayBySeconds(0.02f, () =>
                                {
                                    if (cHandle.IsValid)
                                    {
                                        if (cHandle.Value is CCSPlayerPawn pawn && pawn.IsValid)
                                        {
                                            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                                            pawn.Teleport(null, null, originalVelocity);
                                        }
                                    }
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError("OnEntityIdentityAcceptInputHook: {0} - {1}", ex.Message, ex.Source);
        }
    }

    private CEntityInstance? FindEntityByName(string eName)
    {
        return Core.EntitySystem.GetAllEntities().FirstOrDefault(e => e.IsValid && e.Entity != null && e.Entity.Name.Equals(eName, StringComparison.OrdinalIgnoreCase));
    }

    private static CCSPlayerController? EntityToPlayer(CEntityInstance? entity)
    {
        if (entity != null && entity.IsValid && string.Equals(entity.DesignerName, "player"))
        {
            if (entity is CCSPlayerPawn pawn && pawn.Controller.Value != null && pawn.Controller.Value.IsValid)
            {
                if (pawn.Controller.Value is CCSPlayerController player && player.IsValid) return player;
            }
        }
        return null;
    }

    public string? TryToString(CVariant<CVariantDefaultAllocator> cVariant)
    {
        if (!cVariant.TryGetBool(out var value))
        {
            if (!cVariant.TryGetChar(out var value2))
            {
                if (!cVariant.TryGetInt16(out var value3))
                {
                    if (!cVariant.TryGetUInt16(out var value4))
                    {
                        if (!cVariant.TryGetInt32(out var value5))
                        {
                            if (!cVariant.TryGetUInt32(out var value6))
                            {
                                if (!cVariant.TryGetInt64(out var value7))
                                {
                                    if (!cVariant.TryGetUInt64(out var value8))
                                    {
                                        if (!cVariant.TryGetFloat(out var value9))
                                        {
                                            if (!cVariant.TryGetDouble(out var value10))
                                            {
                                                if (!cVariant.TryGetResourceHandle(out var value11))
                                                {
                                                    if (!cVariant.TryGetUtlStringToken(out var value12))
                                                    {
                                                        if (!cVariant.TryGetHScript(out var value13))
                                                        {
                                                            if (!cVariant.TryGetCHandle(out CHandle<CBaseEntity> value14))
                                                            {
                                                                if (!cVariant.TryGetVector2D(out var value15))
                                                                {
                                                                    if (!cVariant.TryGetVector(out var value16))
                                                                    {
                                                                        if (!cVariant.TryGetVector4D(out var value17))
                                                                        {
                                                                            if (!cVariant.TryGetQAngle(out var value18))
                                                                            {
                                                                                if (!cVariant.TryGetQuaternion(out var value19))
                                                                                {
                                                                                    if (!cVariant.TryGetColor(out var value20))
                                                                                    {
                                                                                        if (!cVariant.TryGetString(out string? value21))
                                                                                        {
                                                                                            return string.Empty;
                                                                                        }

                                                                                        return value21;
                                                                                    }

                                                                                    return $"{value20}";
                                                                                }

                                                                                return $"{value19}";
                                                                            }

                                                                            return $"{value18}";
                                                                        }

                                                                        return $"{value17}";
                                                                    }

                                                                    return $"{value16}";
                                                                }

                                                                return $"{value15}";
                                                            }

                                                            return $"{value14.Raw}";
                                                        }

                                                        return $"{value13}";
                                                    }

                                                    return $"{value12}";
                                                }

                                                return $"{value11}";
                                            }

                                            return $"{value10}";
                                        }

                                        return $"{value9}";
                                    }

                                    return $"{value8}";
                                }

                                return $"{value7}";
                            }

                            return $"{value6}";
                        }

                        return $"{value5}";
                    }

                    return $"{value4}";
                }

                return $"{value3}";
            }

            return $"{value2}";
        }

        return $"{value}";
    }
}
