using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace __Scripts
{
    public class Prefabs : MonoBehaviour
    {
        public static Prefabs ProjectPrefabs;
        public GameObject LaserProjectile => laserProjectiles[indexLP];
        [SerializeField] private GameObject[] laserProjectiles;
        [SerializeField] private GameObject[] missileProjectiles;
        [SerializeField] private TextMeshProUGUI prefabNo;
        
        
        private void OnTransformChildrenChanged()
        {
            throw new NotImplementedException();
        }

        private int indexLP;
        public GameObject[] LaserProjectiles => laserProjectiles;

        public static Prefabs Fetch
        {
            get
            {
                if (ProjectPrefabs == null) ProjectPrefabs = FindObjectOfType<Prefabs>();
                return ProjectPrefabs;
            }
        }

        public void SetLaserProjectileIndex(int index)
        {
            if (index >= 0 && index < laserProjectiles.Length) indexLP = index;
            ProjectilePool.Instance.SetProjectileType(LaserProjectiles[indexLP]);
        }

        public void ShowProjectileNumber()
        {
            prefabNo.text = (1 + indexLP).ToString();
        }
        
    }
}
