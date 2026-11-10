using UnityEngine;
using System.Collections.Generic;

public class SpecialShooter : MonoBehaviour
{
    [SerializeField] private List<GameObject> shootPoints = new(),energyBalls = new(),energyBallParts = new();
    [SerializeField] private GameObject prefabEnergyBall;
    private Animator anim;
    private GameObject energyBall;
    [SerializeField, Min(1)] private int shootCount = 3;
    private int listNumber = -1;
    [SerializeField] private float ShootSpeed = 10f;
    [SerializeField] private Vector3 beforeScale = new Vector3(0f,0f,0f),afterScale = new Vector3(3f,3f,3f);
    [SerializeField] private AudioClip chargeClip, shootClip;



    [Header("チャージ・発射の3D音声")]
    [SerializeField, Range(0f, 5f)] private float chargeVolume = 1f;
    [SerializeField, Range(0f, 5f)] private float shootVolume = 1f;
    [SerializeField, Min(0.1f)] private float soundMinDistance = 10f;
    [SerializeField, Min(0.1f)] private float soundMaxDistance = 100f;
    private AudioSource chargeSource;
    private AudioSource shootSource;

    [Header("ボス用の特殊弾発射地点")]
    [Tooltip("未設定の場合は既存のShoot Pointsから選びます。")]
    [SerializeField] private List<Transform> specialPoints = new();
    private readonly Dictionary<GameObject, Vector3> aimedDirections = new();

    // 以前の呼び出しとの互換用。最寄り1地点から準備する。
    public bool EnergyBallChargeNear(Vector3 targetPosition)
    {
        return ChargeNearestPoints(targetPosition, 1);
    }

    // BossController用。近い順にShoot Count個の地点を選ぶ。
    public bool EnergyBallChargeNearest(Vector3 targetPosition)
    {
        return ChargeNearestPoints(targetPosition, Mathf.Max(1, shootCount));
    }

    private bool ChargeNearestPoints(Vector3 targetPosition, int count)
    {
        List<Transform> candidates = new();
        foreach (Transform point in specialPoints)
            if (point != null && !candidates.Contains(point)) candidates.Add(point);
        if (candidates.Count == 0)
        {
            foreach (GameObject point in shootPoints)
                if (point != null && !candidates.Contains(point.transform))
                    candidates.Add(point.transform);
        }
        if (candidates.Count == 0 || prefabEnergyBall == null ||
            prefabEnergyBall.GetComponent<Rigidbody>() == null)
        {
            Debug.LogError("特殊弾の発射地点と、Rigidbody付きPrefabを設定してください。", this);
            return false;
        }
        candidates.Sort((a, b) =>
            (a.position - targetPosition).sqrMagnitude.CompareTo(
                (b.position - targetPosition).sqrMagnitude));
        EnergyBallDestroy();
        int number = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < number; i++)
        {
            Transform point = candidates[i];
            GameObject ball = Instantiate(prefabEnergyBall, point.position, Quaternion.identity);
            ball.SetActive(true);
            SetBallOwner(ball);
            Animator ballAnimator = ball.GetComponent<Animator>();
            if (ballAnimator != null) ballAnimator.Play("ChangeScale");
            energyBalls.Add(ball);
            // チャージ開始時の位置を狙う。発射後の追尾はしない。
            Vector3 aim = targetPosition - point.position;
            aimedDirections[ball] = aim.sqrMagnitude > 0.0001f ? aim.normalized : transform.right;
        }
        if (chargeClip != null)
        {
            EnsureAudioSources();
            chargeSource.Stop();
            chargeSource.PlayOneShot(chargeClip, Mathf.Clamp(chargeVolume, 0f, 5f));
        }
        return true;
    }

    private void SetBallOwner(GameObject ball)
    {
        bool bossAttack = GetComponentInParent<BossController>() != null;
        foreach (EnergyBallManager manager in ball.GetComponentsInChildren<EnergyBallManager>(true))
            manager.InitializeOwner(bossAttack);
    }

    private void Awake()
    {
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (chargeSource == null) chargeSource = CreateSoundSource("EnergyChargeAudio");
        if (shootSource == null) shootSource = CreateSoundSource("EnergyShootAudio");
        foreach (AudioSource source in new[] { chargeSource, shootSource })
        {
            source.minDistance = Mathf.Max(0.1f, soundMinDistance);
            source.maxDistance = Mathf.Max(source.minDistance, soundMaxDistance);
        }
    }

    private AudioSource CreateSoundSource(string objectName)
    {
        GameObject soundObject = new GameObject(objectName);
        soundObject.transform.SetParent(transform, false);
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 1f;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    private void OnDisable()
    {
        if (chargeSource != null) chargeSource.Stop();
        if (shootSource != null) shootSource.Stop();
    }

    public void EnergyBallCharge()
    {
        if (prefabEnergyBall == null)
        {
            Debug.LogError("エネルギーボールのPrefabが設定されていません。", this);
            return;
        }

        if (shootPoints == null || shootPoints.Count == 0)
        {
            Debug.LogError("発射地点が設定されていません。", this);
            return;
        }

        energyBalls.RemoveAll(obj => obj == null);
        bool createdBall = false;

        for(int i = 0;i < shootCount; i++)
        {
            GetRandom();

            if (shootPoints[listNumber] == null)
            {
                Debug.LogWarning($"発射地点 {listNumber} が設定されていません。", this);
                continue;
            }

            energyBall = Instantiate(prefabEnergyBall,shootPoints[listNumber].transform.position,Quaternion.identity);
            energyBall.SetActive(true);
            SetBallOwner(energyBall);
            anim = energyBall.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("ChangeScale");
            }
            //Debug.Log("準備開始");
            energyBalls.Add(energyBall);
            createdBall = true;
            
        }
        if (createdBall && chargeClip != null)
        {
            EnsureAudioSources();
            chargeSource.Stop();
            chargeSource.PlayOneShot(chargeClip, Mathf.Clamp(chargeVolume, 0f, 5f));
        }
        //EnergyBallShoot();
        
        
    }
    public void EnergyBallShoot()
    {
        if (chargeSource != null) chargeSource.Stop();
        energyBalls.RemoveAll(obj => obj == null);
        bool firedBall = false;
        foreach(GameObject gb in energyBalls)
        {
            if (gb == null)
            {
                continue;
            }

            Rigidbody rigidbody = gb.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                Debug.LogWarning("エネルギーボールにRigidbodyがありません。", gb);
                continue;
            }

            Vector3 shootDirection = transform.right;
            if (aimedDirections.TryGetValue(gb, out Vector3 aimedDirection))
            {
                shootDirection = aimedDirection;
                rigidbody.isKinematic = false;
                rigidbody.constraints &= ~(RigidbodyConstraints.FreezePositionX |
                    RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ);
            }
            rigidbody.AddForce(shootDirection * ShootSpeed, ForceMode.Impulse);
            Destroy(gb,10f);
            firedBall = true;
            //Debug.Log("発射");
            
        }

        if (firedBall && shootClip != null)
        {
            EnsureAudioSources();
            shootSource.PlayOneShot(shootClip, Mathf.Clamp(shootVolume, 0f, 5f));
        }
        
        energyBalls.Clear();
        aimedDirections.Clear();
        //Debug.Log("発射終了");
    }
    public void EnergyBallDestroy()
    {
        aimedDirections.Clear();
        if (chargeSource != null) chargeSource.Stop();
        if(energyBalls != null)
        {
            foreach(GameObject i in energyBalls)
            {
                if (i != null)
                {
                    Debug.Log(i+"を破壊したぞ");
                    Destroy(i);
                }
            }

            energyBalls.Clear();
        }
    }
    private void GetRandom()
    {
        if (shootPoints.Count == 1)
        {
            listNumber = 0;
            return;
        }

        int nextNumber = listNumber < 0
            ? Random.Range(0, shootPoints.Count)
            : Random.Range(0, shootPoints.Count - 1);

        if (listNumber >= 0 && nextNumber >= listNumber)
        {
            nextNumber++;
        }

        listNumber = nextNumber;
    }

}
