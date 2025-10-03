using System;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

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

            MonoModHooks.Modify(deerclopsAI, (il) =>
            {
                var c = new ILCursor(il);

                if (!c.TryGotoNext(MoveType.Before, i => i.MatchCall(spawnPassiveShadowHands)))
                {
                    throw new InvalidProgramException("Couldn't find call to SpawnPassiveShadowHands in AI_123_Deerclops");
                }

                c.Remove();

                // Pop params
                for (var i = 0; i < spawnPassiveShadowHands.GetParameters().Length; i++)
                {
                    c.EmitPop();
                }

                // Pop instance
                c.EmitPop();
            });
        }
    }
}
