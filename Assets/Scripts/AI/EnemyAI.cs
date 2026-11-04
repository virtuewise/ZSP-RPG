using UnityEngine;

namespace ZSP.AI
{
    using ZSP.Player;

    /// <summary>
    /// Simple patrol/chase/attack enemy for the vertical slice.
    /// Uses waypoints if assigned, otherwise wanders around the spawn point.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("Patrol")]
        public Transform[] waypoints;
        public float moveSpeed = 3.2f;
        public float chaseSpeed = 5.1f;
        public float arriveRadius = 0.6f;

        [Header("Aggro")]
        public float aggroRange = 20f;
        public float attackRange = 1.9f;
        public int attackDamage = 1;
        public float attackCooldown = 1.1f;
        public LayerMask sightMask = ~0;

        [Header("SIS")]
        public float detectionMultiplier = 1f;

        private Transform _player;
        private CharacterController _controller;
        private Vector3 _spawn;
        private int _waypointIndex;
        private float _nextAttack;
        private bool _chasing;
        private readonly RaycastHit[] _sightHits = new RaycastHit[16];

        private void Awake()
        {
            _spawn = transform.position;
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.height = 1.8f;
                _controller.radius = 0.35f;
                _controller.center = new Vector3(0f, 0.9f, 0f);
            }
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) _player = playerObject.transform;
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (_player == null) return;
            }

            var toPlayer = _player.position - transform.position;
            var distance = toPlayer.magnitude;
            _chasing = distance <= aggroRange * detectionMultiplier && HasLineOfSight(_player.position);

            if (_chasing)
            {
                if (distance > attackRange)
                {
                    MoveTowards(_player.position, chaseSpeed);
                }
                else if (Time.time >= _nextAttack)
                {
                    _nextAttack = Time.time + attackCooldown;
                    var health = _player.GetComponent<PlayerHealth>();
                    if (health != null) health.Damage(attackDamage);
                    Debug.Log($"[ZSP] {name} attacks the operative.");
                }
            }
            else
            {
                var patrolTarget = NextPatrolTarget();
                if ((patrolTarget - transform.position).magnitude > arriveRadius)
                {
                    MoveTowards(patrolTarget, moveSpeed);
                }
            }
        }

        private Vector3 NextPatrolTarget()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                // Lazy wander: a slow circle around the spawn point, offset by hash so squads don't stack.
                var phase = (GetHashCode() & 0xFFFF) / 65535f * Mathf.PI * 2f;
                var t = Time.time * 0.18f + phase;
                return _spawn + new Vector3(Mathf.Cos(t) * 6f, 0f, Mathf.Sin(t) * 6f);
            }

            var target = waypoints[_waypointIndex];
            if (target == null)
            {
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
                return NextPatrolTarget();
            }
            if ((target.position - transform.position).magnitude <= arriveRadius)
            {
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            }
            return target.position;
        }

        private void MoveTowards(Vector3 destination, float speed)
        {
            var flat = destination - transform.position;
            flat.y = 0f;
            var magnitude = flat.magnitude;
            if (magnitude < 0.01f) return;
            var direction = flat / magnitude;
            var step = direction * speed * Time.deltaTime;
            _controller.Move(step);
            var face = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 10f * Time.deltaTime);
        }

        private bool HasLineOfSight(Vector3 target)
        {
            var origin = transform.position + Vector3.up * 1.4f;
            var direction = target + Vector3.up * 1.2f - origin;
            if (direction.sqrMagnitude < 0.01f) return true;
            // Ignore the enemy and target's own colliders; only world geometry blocks sight.
            int hitCount = Physics.RaycastNonAlloc(origin, direction.normalized, _sightHits,
                direction.magnitude, sightMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = _sightHits[i].collider;
                if (collider == null) continue;
                var root = collider.transform.root;
                if (root == transform.root || (_player != null && root == _player.root)) continue;
                return false;
            }
            return true;
        }
    }
}
