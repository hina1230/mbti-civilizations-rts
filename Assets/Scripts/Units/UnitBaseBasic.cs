using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MBTICivilizations.Units
{
    public abstract class UnitBaseBasic : MonoBehaviour, ISelectable, IDamageable
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
        
        [Header("Selection")]
        [SerializeField] protected GameObject selectionIndicator;
        protected bool isSelected = false;
        
        [Header("Ownership")]
        protected string ownerPlayerId = "LocalPlayer";
        protected int teamId = 0;
        
        [Header("State")]
        protected UnitState currentState = UnitState.Idle;
        protected float currentHealth;
        protected bool isAlive = true;

        protected virtual void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            
            currentStats = baseStats.Clone();
            currentHealth = baseStats.maxHealth;
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
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0, 1, 0, 0.3f);
                    renderer.material = mat;
                }
                
                selectionIndicator = indicator;
                selectionIndicator.SetActive(false);
            }
        }

        protected virtual void Start()
        {
            ConfigureNavAgent();
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
            if (!isAlive) return;
            
            HandleInput();
            UpdateUnitBehavior();
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
                        SetTarget(hit.collider.gameObject);
                    }
                    else if (hit.collider.CompareTag("Ground"))
                    {
                        MoveToPosition(hit.point);
                    }
                }
            }
        }

        protected virtual void UpdateUnitBehavior()
        {
            switch (currentState)
            {
                case UnitState.Idle:
                    LookForEnemies();
                    break;
                case UnitState.Moving:
                    if (navAgent != null && navAgent.remainingDistance < 0.1f)
                    {
                        currentState = UnitState.Idle;
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
                    currentState = UnitState.Attacking;
                }
            }
        }

        protected GameObject GetNearestEnemy(Collider[] enemies)
        {
            GameObject nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                UnitBaseBasic enemyUnit = enemy.GetComponent<UnitBaseBasic>();
                if (enemyUnit != null && enemyUnit.teamId != teamId && enemyUnit.isAlive)
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
                currentState = UnitState.Idle;
                return;
            }
            
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            if (distance > attackRange)
            {
                if (navAgent != null && navAgent.enabled)
                {
                    navAgent.SetDestination(currentTarget.transform.position);
                    currentState = UnitState.Moving;
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
                Debug.Log($"{unitName} attacks {target.name}!");
            }
        }

        protected virtual void GatherResources()
        {
            // Basic resource gathering implementation
        }

        public void MoveToPosition(Vector3 position)
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(position);
                currentState = UnitState.Moving;
                currentTarget = null;
            }
        }

        public void SetTarget(GameObject target)
        {
            currentTarget = target;
            currentState = UnitState.Attacking;
        }

        public void Stop()
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.ResetPath();
            }
            currentState = UnitState.Idle;
            currentTarget = null;
        }

        public void TakeDamage(float damage)
        {
            float actualDamage = damage - currentStats.armor;
            actualDamage = Mathf.Max(actualDamage, 1f);
            
            currentHealth -= actualDamage;
            
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                Debug.Log($"{unitName} takes {actualDamage} damage!");
            }
        }

        protected virtual void Die()
        {
            isAlive = false;
            currentState = UnitState.Dead;
            Debug.Log($"{unitName} has been destroyed!");
            
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
            
            StartCoroutine(DestroyAfterDelay());
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            Destroy(gameObject);
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

        public void SetOwner(string playerId, int team)
        {
            ownerPlayerId = playerId;
            teamId = team;
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