using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NWS_WeaponSkill {
    public class WeaponTags {

        private static String normalTagsKey = "nwsXingChen";
        private static String plusTagsKey = "nwsThrownWeapon";
        private static String allTagsKey = normalTagsKey + "," + plusTagsKey;

        public static FastTags<TagGroup.Global> normalTags = FastTags<TagGroup.Global>.Parse(normalTagsKey);
        public static FastTags<TagGroup.Global> plusTags = FastTags<TagGroup.Global>.Parse(plusTagsKey);
        public static FastTags<TagGroup.Global> allTags = FastTags<TagGroup.Global>.Parse(allTagsKey);
    }
}
