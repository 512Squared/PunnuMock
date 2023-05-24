using System;
using System.Collections.Generic;
using UnityEngine;

namespace __Scripts
{
    public class ProjectilePool : SingletonMonobehaviour<ProjectilePool>
    {
        public GameObject projectilePrefab;
        [SerializeField] private Transform projectilePool;
        

        private void Start()
        {
            projectilePrefab = Prefabs.Fetch.laserProjectile;
        }

        private Queue<GameObject> pool = new Queue<GameObject>();

        public GameObject Get()
        {
            if (pool.Count == 0)
            {
                AddObjects(1);
            }
        
            return pool.Dequeue();
        }

        public void Return(GameObject objectToReturn)
        {
            objectToReturn.SetActive(false);
            pool.Enqueue(objectToReturn);
        }

        private void AddObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject newObject = Instantiate(projectilePrefab, projectilePool);
                newObject.SetActive(false);
                pool.Enqueue(newObject);
            }
        }
    }

}
