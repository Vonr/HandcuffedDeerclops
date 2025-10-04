using System;
using System.ComponentModel;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace HandcuffedDeerclops
{
    public class HandcuffedDeerclops : Mod
    {
        public override void Load()
        {
            ApplyILEdits();
        }

        private static void ApplyILEdits()
        {
            var npc = typeof(NPC);
            var deerclopsAI = npc.GetMethod("AI_123_Deerclops", BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnPassiveShadowHands = npc.GetMethod("SpawnPassiveShadowHands", BindingFlags.NonPublic | BindingFlags.Instance);
            var targetClosest = npc.GetMethod("TargetClosest", BindingFlags.Public | BindingFlags.Instance);
            var randomizeInsanityShadowFor = typeof(Projectile).GetMethod("RandomizeInsanityShadowFor", BindingFlags.Public | BindingFlags.Static);

            IL_NPC.AI_123_Deerclops += (il) =>
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<HandcuffedDeerclops>(), il);

                var c = new ILCursor(il);

                var afterPassive = c.DefineLabel();
                if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(spawnPassiveShadowHands)))
                {
                    throw new InvalidProgramException($"Couldn't find call to {spawnPassiveShadowHands} in AI_123_Deerclops");
                }
                c.MarkLabel(afterPassive);

                if (!c.TryGotoPrev(MoveType.Before, i => i.MatchLdarg0()))
                {
                    throw new InvalidProgramException($"Couldn't find call to ldarg.0 before {spawnPassiveShadowHands} in AI_123_Deerclops");
                }

                c.EmitDelegate(() =>
                {
                    var config = ModContent.GetInstance<HandcuffedDeerclopsConfig>();
                    return config.DisablePassiveHands;
                });
                c.EmitBrtrue(afterPassive);

                if (!c.TryGotoNext(MoveType.Before, i => i.MatchCall(randomizeInsanityShadowFor)))
                {
                    throw new InvalidProgramException($"Couldn't find call to {randomizeInsanityShadowFor} in AI_123_Deerclops");
                }

                if (!c.TryGotoPrev(MoveType.After, i => i.MatchCall(targetClosest)))
                {
                    throw new InvalidProgramException($"Couldn't find call to {targetClosest} in AI_123_Deerclops");
                }

                c.EmitDelegate(() =>
                {
                    var config = ModContent.GetInstance<HandcuffedDeerclopsConfig>();
                    return config.DisableActiveHands;
                });

                var afterActive = c.DefineLabel();
                c.EmitBrtrue(afterActive);

                if (!c.TryGotoNext(MoveType.After, i => i.MatchBlt(out var loopEnd)))
                {
                    throw new InvalidProgramException($"Couldn't find end of active hand summoning loop (blt.s) in AI_123_Deerclops");
                }
                c.MarkLabel(afterActive);
            };
        }
    }

    public class HandcuffedDeerclopsConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(true)]
        public bool DisablePassiveHands { get; set; }

        [DefaultValue(false)]
        public bool DisableActiveHands { get; set; }
    }
}
