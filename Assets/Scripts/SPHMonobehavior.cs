using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class SPHMonobehavior : MonoBehaviour
{
    struct Particle
    {
        public GameObject particleGo;
        public MeshRenderer meshRenderer;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 forces;
        public float density;
        public float pressure;
        public int id;

        public void InitializeParticle(Vector3 _position, GameObject o)
        {
            position = _position;
            particleGo = o;
            meshRenderer = o.GetComponent<MeshRenderer>();
            velocity = Vector3.zero;
            forces = Vector3.zero;
            density = 0.0f;
            pressure = 0.0f;
        }
    }

    struct Collider
    {
        public Vector3 position;
        public Vector3 up;
        public Vector3 right;
        public Vector2 scale;

        public void InitializeCollider(Transform _transform)
        {
            position = _transform.position;
            right = _transform.right;
            up = _transform.up;
            scale = new Vector2(_transform.lossyScale.x / 2.0f, 
                _transform.lossyScale.y / 2.0f);
        }
    }

    private readonly Vector3 GRAVITY = new Vector3(0.0f, -9.81f, 0.0f);
    
    // Müller kernel funcions:
    private float POLY6 = 315f / (64 * Mathf.PI * Mathf.Pow(smoothingRadius, 9.0f));
    private float SPIKY_GRAD = -45.0f / (Mathf.PI * Mathf.Pow(smoothingRadius, 6.0f));
    private float VISC_KER = 15.0f / (2 * Mathf.PI * Mathf.Pow(smoothingRadius, 3.0f));
    
    [Header("Visual inputs:")]
    [SerializeField] private GameObject particlePrefab = null;
    [SerializeField] private Gradient colors = null;
    [HideInInspector] public bool paused = true;

    private void Start()
    {
        _collidersGameObjects = GameObject.FindGameObjectsWithTag("Colliders");
    }

    public void Pause() { paused = !paused;}
    [Header("Simulation parameters")]
    [SerializeField] private int particleQuantity = 100;
    [SerializeField] private int rows = 8;
    [SerializeField] private float particleRadius = .5f;
    [SerializeField] private float particleMass = 0.1f;
    [SerializeField] private float particleDrag = 0.025f;
    private static float smoothingRadius = 1.0f;
    private readonly float _smoothingRadiusSquared = smoothingRadius * smoothingRadius;
    [SerializeField] private float restDensity = 15.0f;
    [SerializeField] private float viscosity = 1.0f;
    [SerializeField] private float gravityMultiplier = 2000.0f;
    [SerializeField] private float GAS_CONSTANT = 2000.0f;
    [SerializeField] private float DT = 0.0008f;
    [SerializeField] private float DAMPING = -0.5f;
    
    private Particle[] _particles;
    private GameObject[] _collidersGameObjects;

    private void OnDisable()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            Destroy(_particles[i].particleGo);
        }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        InitializeSimulation();
    }

    // Update is called once per frame
    void Update()
    {
        if (paused) return;
        ComputeDensity();
        ComputeForces();
        Integrate();
        ComputeColliders();
        UpdatePositions();
        UpdateColor();
    }

    void InitializeSimulation()
    {
        _particles = new Particle[particleQuantity];
        
        for (int i = 0; i < particleQuantity; i++)
        {
            float jittering = (Random.value * 2.0f - 1.0f) * particleRadius * 0.1f;
            float x = transform.position.x +(i%rows) + Random.Range(-0.1f, 0.1f);
            float y = transform.position.y +(2*particleRadius)+(float)((i/rows)/rows) * 1.1f;
            float z = transform.position.z +((i/rows)%rows) + Random.Range(-0.1f, 0.1f);
            GameObject o = Instantiate(particlePrefab);
            
            o.transform.localScale = Vector3.one * particleRadius;
            o.transform.position = new Vector3(x, y, z);
            _particles[i].InitializeParticle(new Vector3(x+jittering,y,z+jittering), o);
        }
    }

    void ComputeDensity()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i].density = 0.0f; // resets density value.
            for (int j = 0; j < _particles.Length; j++)
            {
                Vector3 rhoIJ = _particles[j].position - _particles[i].position;
                float rhoSqr = rhoIJ.sqrMagnitude;
                if (rhoSqr < _smoothingRadiusSquared)
                {
                    _particles[i].density += particleMass *
                                            POLY6 *
                                            Mathf.Pow(smoothingRadius - rhoSqr, 3.0f);
                }
            }

            _particles[i].pressure = GAS_CONSTANT * (_particles[i].density - restDensity);
        }
    }

    void ComputeForces()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            Vector3 forcePressure = Vector3.zero;
            Vector3 forceViscosity = Vector3.zero;
            for (int j = 0; j < _particles.Length; j++)
            {
                if (i == j) continue;
                Vector3 distIJ = _particles[j].position - _particles[i].position;
                float distSqr = distIJ.sqrMagnitude;
                float dist = Mathf.Sqrt(distSqr); // check distance?
                if (dist < smoothingRadius)
                {
                    forcePressure += -distIJ.normalized * (particleMass * (_particles[i].pressure + _particles[j].pressure)) / (2.0f * _particles[j].density) * (SPIKY_GRAD * (smoothingRadius - dist));
                    forceViscosity += viscosity * particleMass *
                        (_particles[j].velocity - _particles[i].velocity) /
                        _particles[j].density * (VISC_KER * (smoothingRadius - dist));
                                     
                }
            }

            Vector3 forceGravity = GRAVITY * (particleMass * _particles[i].density * gravityMultiplier); // maybe multiply by density?
            _particles[i].forces = forcePressure + forceViscosity + forceGravity;
        }
    }

    void Integrate()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i].velocity += DT * (_particles[i].forces / _particles[i].density);
            _particles[i].position += DT * _particles[i].velocity;
        }
    }

    void UpdatePositions()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i].particleGo.transform.position = _particles[i].position;
        }
    }

    void ComputeColliders()
    {
        Collider[] colliders = new Collider[_collidersGameObjects.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].InitializeCollider(_collidersGameObjects[i].transform);
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            for (int j = 0; j < colliders.Length; j++)
            {
                Vector3 normalPen;
                Vector3 posPen;
                float lenghtPen;
                if (Intersect(colliders[j], _particles[i].position, particleRadius, out normalPen, out posPen, out lenghtPen))
                {
                    _particles[i].velocity =
                        DampVelocity(colliders[j], _particles[i].velocity, normalPen, 1.0f - particleDrag);
                    _particles[i].position = posPen - normalPen * Mathf.Abs(lenghtPen);
                }
            }
        }
    }

    bool Intersect(Collider col, Vector3 pos, float radius, out Vector3 normalPen, out Vector3 posPen,
        out float lenghtPen)
    {
        Vector3 colProject = col.position - pos;                                                // project collider position
        normalPen = Vector3.Cross(col.right, col.up);                                     // get cross product between right and up vector component 
        lenghtPen = Mathf.Abs(Vector3.Dot(colProject, normalPen)) - (radius / 2.0f);    // get magnitude/length of vector connecting projected vector and normal
        posPen = col.position - colProject;                                                     // update vector position
        return lenghtPen < 0.0f &&                                                              // if (i)   magnitude is 0 or less
               Mathf.Abs(Vector3.Dot(colProject, col.right)) < col.scale.x &&           //    (ii)  absolute value of dot product between projected and right is less then x-scale (assures collision in x-axis)
               Mathf.Abs(Vector3.Dot(colProject, col.up)) < col.scale.y;                //    (iii) same, but for y-axis. If all, then collided: answer by damping.
    }

    Vector3 DampVelocity(Collider col, Vector3 vel, Vector3 penNormal, float drag)
    {
        Vector3 newVel = Vector3.Dot(vel, penNormal) * penNormal * DAMPING              
                         + Vector3.Dot(vel, col.right) * col.right * drag
                         + Vector3.Dot(vel, col.up) * col.up * drag;
        newVel = Vector3.Dot(newVel, Vector3.forward) * Vector3.forward
                 + Vector3.Dot(newVel, Vector3.right) * Vector3.right
                 + Vector3.Dot(newVel, Vector3.up) * Vector3.up;
        return newVel;
    }

    void UpdateColor()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            float gradient = (_particles[i].velocity.magnitude / 400); // this gives 0-1 range of velocity
            _particles[i].meshRenderer.material.color = colors.Evaluate(gradient);
        }
    }
}
