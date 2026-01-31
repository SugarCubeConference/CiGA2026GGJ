using System.Collections.Generic;
using UnityEngine;

namespace MaskGame.Data
{
    [CreateAssetMenu(fileName = "EncounterSet", menuName = "Mask Game/Encounter Set")]
    public class EncounterSet : ScriptableObject
    {
        public List<EncounterData> items = new List<EncounterData>();
    }
}
