using System;
using System.Collections.Generic;
using UnityEngine;

namespace __Scripts
{
    public class TurretsManager : SingletonMonobehaviour<TurretsManager>
    {
        [SerializeField] private List<Turret> turretList;

        public List<Turret> TurretList
        {
            get => turretList;
            set => turretList = value;
        }

        public List<Turret> GetTurretList()
        {
            return TurretList;
        }
    }
}
