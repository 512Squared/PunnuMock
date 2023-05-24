using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace __Scripts
{
    public class Turret : MonoBehaviour
    {
        #region Fields & Properties

        [PropertySpace(SpaceBefore = 10)]
        [Tooltip("Switch on for component specific console debugging and to make range and arc gizmos visible during setup")]
        [SerializeField]
        private bool debugOn;

        [Tooltip("Tilts the arc and range gizmo - useful for fine-tuning")] [SerializeField] [ShowIf("debugOn")]
        private float arcTiltAngle;


        [Title("Info Only", horizontalLine: true)] [SerializeField]
        private bool hasTarget;

        [ShowIf("hasTarget")] [SerializeField] private TurretState turretState = TurretState.Idle;
        [ShowIf("hasTarget")] [SerializeField] private bool isObstacleBlocking;
        [ShowIf("hasTarget")] [SerializeField] private bool insideBarrelAngle;
        [ShowIf("hasTarget")] [SerializeField] private bool insideTurretArc;

        [Title("Components", horizontalLine: true)] [SerializeField] [Tooltip("Main Camera - used to call camera shake for hits")]
        public Animation mainCamera;

        [Tooltip("Turret's gun")] [SerializeField]
        private Transform turretGun;

        [Tooltip("Turret's rotating base - gun's parent object")] [SerializeField]
        private Transform turretBase;

        [Tooltip("Turret's top-level parent")] [SerializeField]
        private Transform turretContainer;

        [Tooltip("A transform child for setting exit point for projectiles and muzzle flash")] [SerializeField]
        private Transform muzzleFlashPoint;

        [Title("Settings", horizontalLine: true)] [Tooltip("Layer for targets - usually Player")] [SerializeField]
        private LayerMask targetMask;

        [Tooltip("Layer for obstacles in the scene that Player can hide behind")] [SerializeField]
        private LayerMask obstacleMask;

        [Tooltip("Turret's range")] [SerializeField]
        private float range = 10f;

        [Tooltip("Width of the targeting arc - switch debug on to make visible in Scene view")] [SerializeField]
        private float turretTargetArc;

        [Tooltip("An area just below the turret that is a safe zone creating by barrel minimum elevation")] [SerializeField]
        private float safeZoneRange = 10f;

        [Tooltip("Vertical arc for gun barrel - used for targeting")] [SerializeField]
        private float turretFiringAngle = 30f;

        [Tooltip("Used for fine-tuning gun direction when targeting")] [SerializeField]
        private float turretGunElevationOffset;

        [Tooltip("Cooldown between attacks")] [SerializeField]
        private float attackCooldown = 1f;

        [Title("Projectile", horizontalLine: true)] [Tooltip("Speed of the projectile - increase to hit moving target")] [SerializeField]
        private float laserProjectileSpeed;

        [Tooltip("Projectile damage - value is passed to turret projectile on instantiation")] [SerializeField]
        private int damage = 10;

        [Tooltip("Add targeting variance")] [SerializeField]
        private bool spreadBullets;

        [Tooltip("Amount of variance to add +-")] [SerializeField] [ShowIf("spreadBullets")]
        private Vector3 bulletSpread = new(0.1f, 0.1f, 0.1f);

        // non-visible members
        private Vector3 gunOrigin;
        private Quaternion defaultRotationBase;
        private Quaternion defaultRotationBarrel;
        private Renderer lastObstacleRenderer;
        private Transform lastObstacle;
        private Color lastObstacleColor;
        private Color debugObstacleColor = Color.yellow;
        private float timeSinceLastAttack;
        public int selectedProjectileIndex = 0;

        public bool HasTarget => hasTarget;

        // FSM for turret's firing system
        private enum TurretState
        {
            Idle,
            Aiming,
            ReadyToFire,
            Attacking
        }

        #endregion

        #region Callbacks

        private void Start()
        {
            Cursor.visible = false;
            defaultRotationBase = transform.rotation;
            defaultRotationBarrel = turretGun.localRotation;
            Prefabs.Fetch.SetLaserProjectileIndex(selectedProjectileIndex);
        }

        /// <summary>
        /// Check for targets within range - use a HeadTransform component to set exact impact point on targets
        /// </summary>
        private void Update()
        {
            timeSinceLastAttack += Time.deltaTime;
            hasTarget = true;
            Collider[] targetsInRange = Physics.OverlapSphere(transform.position, range, targetMask);

            if (targetsInRange.Length < 1)
            {
                hasTarget = false;
                isObstacleBlocking = false;
                turretState = TurretState.Idle;
                ReturnGunToDefaultPosition();
                if (lastObstacleRenderer != null)
                {
                    lastObstacleRenderer.material.color = lastObstacleColor;
                    lastObstacleRenderer = null;
                }

                return;
            }

            foreach (Collider target in targetsInRange)
            {
                if (target.CompareTag("Player"))
                {
                    if (turretState != TurretState.ReadyToFire) AimAtTarget(targetsInRange[0].transform);

                    HeadTransform headTransform = target.GetComponentInChildren<HeadTransform>(); // optional for fine-tuning impact point on target
                    Vector3 impactPoint = headTransform ? headTransform.transform.position : target.transform.position;

                    isObstacleBlocking = ProcessTargetingFSM(target, impactPoint);

                    Debug.Log(
                        $"TARGET availability: State - Angle - Arc - Obstacle \nFSM : {turretState} | TRUE :  {insideBarrelAngle} | TRUE : {insideTurretArc} | FALSE : {isObstacleBlocking}");

                    if ((target.transform.position - transform.position).magnitude < safeZoneRange)
                    {
                        hasTarget = false;
                        break; // abort attack sequence if inside safe zone below turret
                    }

                    if (turretState == TurretState.ReadyToFire && insideTurretArc && insideBarrelAngle && !isObstacleBlocking)
                    {
                        PerformAttack(target.transform);
                        Debug.Log($"Perform Attack called");
                        turretState = TurretState.Attacking;
                    }

                    if (turretState is TurretState.Attacking or TurretState.ReadyToFire)
                    {
                        hasTarget = true;
                        break;
                    }
                }

                insideBarrelAngle = true;
                insideTurretArc = IsTargetInAttackArc(target.transform);

                // If target is not inside the turret arc, reset 'hasTarget' to false
                if (!insideTurretArc) hasTarget = false;
            }
        }

        #endregion

        #region Main Methods

        /// <summary>
        /// Calculates horizontal direction to target & rotates turret base horizontally, then calculates vertical direction of barrel to target & rotates barrel vertically. Tween provides for smooth movement. Separating out the horizontal and vertical movements gives natural turret-like behaviour.  
        /// </summary>
        /// <param name="target"></param>
        private void AimAtTarget(Transform target)
        {
            // Rotate the turret base horizontally to face the target
            Vector3 directionToTargetHorizontal = (new Vector3(target.position.x, turretBase.position.y, target.position.z) - turretBase.position).normalized;
            Quaternion targetRotationHorizontal = Quaternion.LookRotation(directionToTargetHorizontal);
            turretBase.DORotateQuaternion(targetRotationHorizontal, 0.5f);

            HeadTransform headTransform = target.GetComponentInChildren<HeadTransform>(); // optional for fine-tuning impact point on target
            Vector3 impactPoint = headTransform ? headTransform.transform.position : target.transform.position;

            // Rotate the turret barrel vertically to aim at the target
            Vector3 directionToTargetFromBarrel = (impactPoint - turretGun.position).normalized;

            // We create an offset vector for fine-tuning
            Vector3 offsetVector = directionToTargetFromBarrel * turretGunElevationOffset;

            // We create the final aim vector, applying the offset
            Vector3 finalAimVector = impactPoint + offsetVector;

            // Use LookAt tween to aim the turretGun using the final aim vector - set the FSM
            turretGun.DOLookAt(finalAimVector, 1f)
                .OnComplete(() =>
                {
                    turretState = TurretState.ReadyToFire;
                });
        }


        /// <summary>
        /// Checks if target is within turret firing angle and checks for potential obstacles
        /// </summary>
        /// <param name="target"></param>
        /// <param name="aimAtHead"></param>
        private bool ProcessTargetingFSM(Collider target, Vector3 aimAtHead)
        {
            Vector3 dirToTarget = (aimAtHead - turretGun.position).normalized;
            float targetDistance = Vector3.Distance(turretGun.position, aimAtHead);

            float targetElevationAngle = Mathf.Asin(dirToTarget.y) * Mathf.Rad2Deg;

            // Check if the target is in the vertical safe zone TODO: After adding safeZoneArc, this might be redundant (refactor?)
            if (targetElevationAngle < turretFiringAngle)
            {
                if (debugOn) Debug.Log("Target is in the safe zone.");
                hasTarget = false;
                insideBarrelAngle = false;
                return false; // treat this as an obstacle is the easiest
            }

            insideBarrelAngle = true;
            insideTurretArc = IsTargetInAttackArc(target.transform);

            if (Physics.Raycast(turretGun.position, dirToTarget, out RaycastHit hitInfo, range, obstacleMask))
            {
                float obstacleDistance = hitInfo.distance;
                if (obstacleDistance < targetDistance)
                {
                    HasObstacle(hitInfo.transform, dirToTarget);
                    Debug.Log($"Object between turret and Player: {hitInfo.transform.name}");
                    return true; // obstacleBlocking
                }
            }

            if (lastObstacle != null) ClearLastObstacleColoration();
            switch (debugOn)
            {
                case true when insideTurretArc:
                    Debug.DrawRay(turretGun.position, dirToTarget * range, Color.green);
                    break;
                case true:
                    Debug.DrawRay(turretGun.position, dirToTarget * range, Color.yellow);
                    break;
            }

            return false; // obstacleBlocking
        }

        private bool IsTargetInAttackArc(Transform target)
        {
            Vector3 directionToTarget = (target.position - turretContainer.position).normalized;
            float angle = Vector3.Angle(turretContainer.forward, directionToTarget);
            return Mathf.Abs(angle) <= turretTargetArc / 2;
        }

        private void PerformAttack(Transform target)
        {
            bool attack;
            if (timeSinceLastAttack >= attackCooldown)
            {
                attack = true;
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null) Shoot();
                timeSinceLastAttack = 0;
                turretState = TurretState.Aiming;
                if (debugOn) Debug.Log($"ATTACK carried out | Turret: {transform.name}");
            }
            else
            {
                attack = false;
                if (debugOn) Debug.Log($"Still in COOLDOWN");
            }

            if (debugOn) Debug.Log($"PerformAttack called on target: {target.name} | Attack: {attack}");
        }

        /// <summary>
        /// Shoot() makes use of object pooling via ProjectPool on the Turret Manager game object. Targeting variance is also applied here (initial value 10%)
        /// </summary>
        private void Shoot()
        {
            if (debugOn) Debug.Log($"SHOOT called");
            mainCamera.Play(mainCamera.clip.name);

            GameObject newProjectile = ProjectilePool.Instance.Get();
            Projectile projectile = newProjectile.GetComponent<Projectile>();
            projectile.SetDamageAmount(damage);

            ParticleSystem ps = newProjectile.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startSpeed = laserProjectileSpeed;

            newProjectile.transform.position = muzzleFlashPoint.position; // Set the initial position to the muzzle's position
            newProjectile.transform.forward = spreadBullets ? AddVariance(muzzleFlashPoint.forward) : muzzleFlashPoint.forward; // Set the direction
            newProjectile.transform.rotation = muzzleFlashPoint.rotation;
            newProjectile.SetActive(true);
            ps.Play();
            StartCoroutine(ReturnToPoolAfterDelay(newProjectile, 5f));
        }

        #endregion

        #region Helper methods

        IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ProjectilePool.Instance.Return(obj);
        }

        public bool CanAttackTarget() => hasTarget;

        private void HasObstacle(Component obstacle, Vector3 dirToTarget)
        {
            isObstacleBlocking = true;
            hasTarget = false;

            if (obstacle.transform != lastObstacle)
            {
                if (lastObstacleRenderer != null)
                {
                    lastObstacleRenderer.material.color = lastObstacleColor;
                    lastObstacleRenderer = null;
                }

                SetNewObstacleColoration(obstacle.transform);
            }

            if (lastObstacle == null) SetNewObstacleColoration(obstacle.transform);

            if (debugOn) Debug.DrawRay(turretGun.position, dirToTarget * range, Color.red);
        }

        private void SetNewObstacleColoration(Transform newObstacle)
        {
            lastObstacle = newObstacle;
            lastObstacleRenderer = lastObstacle.GetComponent<Renderer>();
            if (lastObstacleRenderer != null)
            {
                Debug.Log($"Setting new obstacle: {newObstacle.name}");
                lastObstacleColor = lastObstacleRenderer.material.color;
                if (debugOn) lastObstacleRenderer.material.color = debugObstacleColor;
            }
            else
            {
                Debug.Log($"No Renderer found on the new obstacle: {newObstacle.name}");
            }
        }

        private void ClearLastObstacleColoration()
        {
            if (lastObstacleRenderer != null)
            {
                lastObstacleRenderer.material.color = lastObstacleColor;
                lastObstacleRenderer = null;
            }

            lastObstacle = null;
        }


        /// <summary>
        /// Rotates base back to default and raises barrel to default
        /// </summary>
        private void ReturnGunToDefaultPosition()
        {
            transform.DORotateQuaternion(defaultRotationBase, 2f);
            turretGun.DOLocalRotateQuaternion(defaultRotationBarrel, 2f);
        }

        /// <summary>
        /// Forward position of turret gun alters with turret movement. This returns turret gun's latest forward direction (aimed at player) and adds in some variance to the bullet trajectory if spreadBullets = true. 
        /// </summary>
        /// <returns></returns>
        private Vector3 AddVariance(Vector3 direction)
        {
            Vector3 variance = new (
                Random.Range(-bulletSpread.x, bulletSpread.x),
                Random.Range(-bulletSpread.y, bulletSpread.y),
                Random.Range(-bulletSpread.z, bulletSpread.z));

            direction += variance;

            return direction.normalized;
        }

        [HorizontalGroup("Split", 0.5f)][Tooltip("Cycle through the projectiles prefab list")]
        [Button(ButtonSizes.Large, Icon = SdfIconType.NodeMinus), GUIColor(0.8f, 0.5f, 0.17f)]
        public void PreviousProjectile()
        {
            if (selectedProjectileIndex - 1 >= 0)
            {
                selectedProjectileIndex--;
                Prefabs.Fetch.SetLaserProjectileIndex(selectedProjectileIndex);
            }
        }

        [HorizontalGroup("Split", 0.5f)][Tooltip("Cycle through the projectiles prefab list")]
        [Button(ButtonSizes.Large, Icon = SdfIconType.NodePlus), GUIColor(1f, 1f, 0.215f)]
        public void NextProjectile()
        {
            if (selectedProjectileIndex + 1 >= 0 && selectedProjectileIndex + 1 < Prefabs.Fetch.LaserProjectiles.Length)
            {
                selectedProjectileIndex++;
                Prefabs.Fetch.SetLaserProjectileIndex(selectedProjectileIndex);
            }
        }

        #endregion

        #region Gizmos

        /// <summary>
        /// Gizmos to help with setting ranges, turret angles and debugging
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!debugOn) return;

            // Draw the turretTargetArc
            for (float i = -turretTargetArc / 2; i < turretTargetArc / 2; i += 1)
            {
                Gizmos.color = Color.red;
                Quaternion rotation = Quaternion.Euler(0, i, arcTiltAngle);
                Vector3 direction = rotation * turretContainer.forward;
                Gizmos.DrawLine(turretContainer.position, turretContainer.position + direction * range);
            }

            // Draw the safeZoneArc
            for (float i = -turretTargetArc / 2; i <= turretTargetArc / 2; i += 1)
            {
                Gizmos.color = Color.green;
                Quaternion rotation = Quaternion.Euler(0, i, arcTiltAngle);
                Vector3 direction = rotation * turretContainer.forward;
                Gizmos.DrawLine(turretContainer.position, turretContainer.position + direction * safeZoneRange);
            }
        }

        #endregion
    }
}
