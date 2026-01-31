using System.Collections.Generic;
using UnityEngine;

namespace MaskGame.Data
{
    [CreateAssetMenu(fileName = "EncounterSet", menuName = "Mask Game/Encounter Set")]
    public class EncounterSet : ScriptableObject
    {
        [SerializeField]
        private List<EncounterData> items = new List<EncounterData>();

        public IReadOnlyList<EncounterData> Items => items;
    }
}
