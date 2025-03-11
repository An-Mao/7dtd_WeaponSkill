using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace NWS_WeaponSkill
{
    public class NWS_WeaponSkill : IModApi {


        private static Harmony harmony = new Harmony("nws.dev.7d2d.nws.weapon");
        public void InitMod(Mod _modInstance) {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
