using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

namespace MBTICivilizations.Units
{
    public abstract class UnitBase : NetworkBehaviour, ISelectable, IDamageable
    {
        [Header("Unit Information")]
        [SerializeField] protected string unitName;
        [SerializeField] protected UnitType unitType;
        [SerializeField] protected Sprite unitIcon;
        
        [Header("Unit Stats")]
        [SerializeField] protected UnitStats baseStats;
        protected UnitStats currentStats;
        
        [Header("Combat")]
        [SerializeField] protected float attackRange = 5f;
        [SerializeField] protected float attackCooldown = 1f;
        [SerializeField] protected LayerMask enemyLayer;
        protected float lastAttackTime = 0f;
        protected GameObject currentTarget;
        
        [Header("Movement")]
        protected NavMeshAgent navAgent;
        protected NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        protected NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        
        [Header("Selection")]
        [SerializeField] protected GameObject selectionIndicator;
        protected bool isSelected = false;
        
        [Header("Ownership")]
        protected NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();
        protected NetworkVariable<int> teamId = new NetworkVariable<int>();
        
        [Header("State")]
        protected NetworkVariable<UnitState> currentState = new NetworkVariable<UnitState>(UnitState.Idle);
        protected NetworkVariable<float> currentHealth = new NetworkVariable<float>();
        protected NetworkVariable<bool> isAlive = new NetworkVariable<bool>(true);

        protected virtual void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            
            currentStats = baseStats.Clone();
            SetupComponents();
        }

        protected virtual void SetupComponents()
        {
            if (selectionIndicator == null)
            {
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                indicator.transform.SetParent(transform);
                indicator.transform.localPosition = new Vector3(0, 0.1f, 0);
                indicator.transform.localScale = new Vector3(2f, 0.1f, 2f);
                
                Renderer renderer = indicator.GetComponent<Renderer>();
                renderer.material.color = new Color(0, 1, 0, 0.3f);
                
                selectionIndicator = indicator;
                selectionIndicator.SetActive(false);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                currentHealth.Value = baseStats.maxHealth;
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
            }
            
            if (IsOwner)
            {
                ConfigureNavAgent();
            }
            else
            {
                if (navAgent != null)
                {
                    navAgent.enabled = false;
                }
            }
        }

        protected virtual void ConfigureNavAgent()
        {
            if (navAgent != null)
            {
                navAgent.speed = currentStats.moveSpeed;
                navAgent.acceleration = 8f;
                navAgent.stoppingDistance = unitType == UnitType.Melee ? 2f : attackRange * 0.8f;
                navAgent.angularSpeed = 120f;
            }
        }

        protected virtual void Update()
        {
            if (!isAlive.Value) return;
            
            if (IsOwner)
            {
                HandleInput();
                UpdateNetworkPosition();
            }
            else
            {
                InterpolatePosition();
            }
            
            if (IsServer)
            {
                UpdateUnitBehavior();
            }
        }

        protected virtual void HandleInput()
        {
            if (isSelected && Input.GetMouseButtonDown(1))
            {
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.CompareTag("Enemy"))
                    {
                        SetTargetServerRpc(hit.collider.gameObject);
                    }
                    else if (hit.collider.CompareTag("Ground"))
                    {
                        MoveToPositionServerRpc(hit.point);
                    }
                }
            }
        }

        protected virtual void UpdateNetworkPosition()
        {
            if (navAgent != null && navAgent.enabled)
            {
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
            }
        }

        protected virtual void InterpolatePosition()
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
        }

        protected virtual void UpdateUnitBehavior()
        {
            switch (currentState.Value)
            {
                case UnitState.Idle:
                    LookForEnemies();
                    break;
                case UnitState.Moving:
                    if (navAgent.remainingDistance < 0.1f)
                    {
                        currentState.Value = UnitState.Idle;
                    }
                    break;
                case UnitState.Attacking:
                    PerformAttack();
                    break;
                case UnitState.Gathering:
                    GatherResources();
                    break;
            }
        }

        protected virtual void LookForEnemies()
        {
            if (unitType == UnitType.Worker) return;
            
            Collider[] enemies = Physics.OverlapSphere(transform.position, currentStats.sightRange, enemyLayer);
            if (enemies.Length > 0)
            {
                GameObject nearestEnemy = GetNearestEnemy(enemies);
                if (nearestEnemy != null)
                {
                    currentTarget = nearestEnemy;
                    currentState.Value = UnitState.Attacking;
                }
            }
        }

        protected GameObject GetNearestEnemy(Collider[] enemies)
        {
            GameObject nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                UnitBase enemyUnit = enemy.GetComponent<UnitBase>();
                if (enemyUnit != null && enemyUnit.teamId.Value != teamId.Value && enemyUnit.isAlive.Value)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = enemy.gameObject;
                    }
                }
            }
            
            return nearest;
        }

        protected virtual void PerformAttack()
        {
            if (currentTarget == null || !currentTarget.activeInHierarchy)
            {
                currentTarget = null;
                currentState.Value = UnitState.Idle;
                return;
            }
            
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            if (distance > attackRange)
            {
                if (navAgent != null && navAgent.enabled)
                {
                    navAgent.SetDestination(currentTarget.transform.position);
                    currentState.Value = UnitState.Moving;
                }
            }
            else
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    Attack(currentTarget);
                    lastAttackTime = Time.time;
                }
            }
        }

        protected virtual void Attack(GameObject target)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(currentStats.attackDamage);
                OnAttackClientRpc(target);
            }
        }

        protected virtual void GatherResources()
        {
        }

        [ServerRpc]
        public void MoveToPositionServerRpc(Vector3 position)
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(position);
                currentState.Value = UnitState.Moving;
                currentTarget = null;
            }
        }

        [ServerRpc]
        public void SetTargetServerRpc(GameObject target)
        {
            currentTarget = target;
            currentState.Value = UnitState.Attacking;
        }

        [ServerRpc]
        public void StopServerRpc()
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.ResetPath();
            }
            currentState.Value = UnitState.Idle;
            currentTarget = null;
        }

        public void TakeDamage(float damage)
        {
            if (!IsServer) return;
            
            float actualDamage = damage - currentStats.armor;
            actualDamage = Mathf.Max(actualDamage, 1f);
            
            currentHealth.Value -= actualDamage;
            
            if (currentHealth.Value <= 0)
            {
                Die();
            }
            else
            {
                OnDamageTakenClientRpc(actualDamage);
            }
        }

        protected virtual void Die()
        {
            if (!IsServer) return;
            
            isAlive.Value = false;
            currentState.Value = UnitState.Dead;
            OnDeathClientRpc();
            
            StartCoroutine(DestroyAfterDelay());
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            NetworkObject.Despawn();
        }

        [ClientRpc]
        protected virtual void OnAttackClientRpc(NetworkObjectReference targetRef)
        {
            Debug.Log($"{unitName} attacks!");
        }

        [ClientRpc]
        protected virtual void OnDamageTakenClientRpc(float damage)
        {
            Debug.Log($"{unitName} takes {damage} damage!");
        }

        [ClientRpc]
        protected virtual void OnDeathClientRpc()
        {
            Debug.Log($"{unitName} has been destroyed!");
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }

        public void Select()
        {
            isSelected = true;
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }
        }

        public void Deselect()
        {
            isSelected = false;
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }

        public bool IsSelected()
        {
            return isSelected;
        }

        public void SetOwner(ulong clientId, int team)
        {
            if (IsServer)
            {
                ownerClientId.Value = clientId;
                teamId.Value = team;
            }
        }

        public void ApplyModifiers(Dictionary<string, float> modifiers)
        {
            if (modifiers.ContainsKey("UnitDamage"))
                currentStats.attackDamage = baseStats.attackDamage * modifiers["UnitDamage"];
            if (modifiers.ContainsKey("UnitDefense"))
                currentStats.armor = baseStats.armor * modifiers["UnitDefense"];
            if (modifiers.ContainsKey("UnitSpeed"))
                currentStats.moveSpeed = baseStats.moveSpeed * modifiers["UnitSpeed"];
            
            if (navAgent != null)
            {
                navAgent.speed = currentStats.moveSpeed;
            }
        }
    }

    [System.Serializable]
    public class UnitStats
    {
        public float maxHealth = 100f;
        public float attackDamage = 10f;
        public float armor = 5f;
        public float moveSpeed = 5f;
        public float sightRange = 10f;
        public float buildTime = 10f;
        public Core.ResourceCost cost;
        
        public UnitStats Clone()
        {
            return new UnitStats
            {
                maxHealth = this.maxHealth,
                attackDamage = this.attackDamage,
                armor = this.armor,
                moveSpeed = this.moveSpeed,
                sightRange = this.sightRange,
                buildTime = this.buildTime,
                cost = this.cost
            };
        }
    }

    public enum UnitType
    {
        Worker,
        Melee,
        Ranged,
        Siege,
        Scout,
        Special
    }

    public enum UnitState
    {
        Idle,
        Moving,
        Attacking,
        Gathering,
        Building,
        Dead
    }

    public interface ISelectable
    {
        void Select();
        void Deselect();
        bool IsSelected();
    }

    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}