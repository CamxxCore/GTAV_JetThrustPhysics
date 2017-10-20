using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetThrustPhysics.MaterialStuff
{
    public class EnvMaterialFX 
    {
        public materials[] Materials { get; }
        public string FXDict { get; set; }
        public string FXName { get; set; }

        public EnvMaterialFX(string fxDict, string fxName, params materials[] materials)
        {
            FXDict = fxDict;
            FXName = fxName;
            Materials = materials;
        }

        public materials this[materials m]
        {
            get
            {
                if (Materials.Any(l => l == m))
                    return Materials.First(l => l == m);
                return materials.none;
            }         
        }
    }
}
