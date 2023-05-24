using UnityEngine;

namespace __Scripts
{
    public class Prefabs : MonoBehaviour
    {
        public static Prefabs ProjectPrefabs;

        public static Prefabs Fetch
        {
            get
            {
                if (ProjectPrefabs == null)
                {
                    ProjectPrefabs = FindObjectOfType<Prefabs>();
                }

                return ProjectPrefabs;
            }
        }

        public GameObject laserProjectile;
    }
}