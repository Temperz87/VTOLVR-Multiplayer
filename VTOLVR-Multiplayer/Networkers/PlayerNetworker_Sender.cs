using System.Collections;
using UnityEngine;

class PlayerNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_Respawn lastMessage;
    public Health health;

    public TempPilotDetacher detacher;
    public GearAnimator[] gears;
    public FloatingOriginShifter shifter;
    public EjectionSeat ejection;

    Coroutine repspawnTimer;

    Transform target;
    Transform ejectorParent;
    Transform canopyParent;
    Vector3 ejectorSeatPos;
    Quaternion ejectorSeatRot;
    Vector3 canopyPos;
    Quaternion canopyRot;

    void Awake()
    {
        lastMessage = new Message_Respawn(networkUID);

        health = GetComponent<Health>();


        if (health == null)
            Debug.LogError("health was null on player " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);

        detacher = GetComponentInChildren<TempPilotDetacher>();
        gears = GetComponentsInChildren<GearAnimator>();
        shifter = GetComponentInChildren<FloatingOriginShifter>();
        ejection = GetComponentInChildren<EjectionSeat>();
        ejection.OnEject.AddListener(Eject);

        ejectorSeatPos = ejection.transform.localPosition;
        ejectorSeatRot = ejection.transform.localRotation;
        Debug.LogError("found health on " + gameObject.name);

        target = detacher.cameraRig.transform.parent;
        ejectorParent = ejection.gameObject.transform.parent;
        if (ejection.canopyObject != null) {
            canopyParent = ejection.canopyObject.transform.parent;
            canopyPos = ejection.canopyObject.transform.localPosition;
            canopyRot = ejection.canopyObject.transform.localRotation;
        }
    }

    IEnumerator RespawnTimer()
    {
        Debug.Log("Starting respawn timer.");

        yield return new WaitForSeconds(15);

        Debug.Log("Finished respawn timer.");

        ReArmingPoint rearmPoint = GameObject.FindObjectOfType<ReArmingPoint>();

        UnEject();
        PutPlayerBackInAircraft();

        foreach (GearAnimator gear in gears) {
            gear.ExtendImmediate();
        }

        GetComponent<Rigidbody>().velocity = Vector3.zero;
        transform.position = rearmPoint.transform.position + Vector3.up * 10;
        transform.rotation = rearmPoint.transform.rotation;

        //rearmPoint.OnEndRearm += GameObject.FindObjectOfType<MFDCommsPage>().CurrentRP_OnEndRearm;
        rearmPoint.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.Success);
        PilotSaveManager.currentScenario.totalBudget = 999999;
        PilotSaveManager.currentScenario.initialSpending = 0;
        PilotSaveManager.currentScenario.inFlightSpending = 0;
        rearmPoint.BeginReArm();
    }

    void UnEject()
    {
        if (ejection.canopyObject)
        {
            ejection.canopyObject.GetComponentInChildren<Collider>().enabled = false;
            ejection.canopyObject.GetComponentInChildren<Collider>().gameObject.layer = 9;//set to debris layer
        }
        ejection.gameObject.GetComponentInChildren<Collider>().enabled = false;
        ejection.gameObject.GetComponentInChildren<Collider>().gameObject.layer = 9;//set to debris layer

        if (ejection.canopyObject)
        {
            Destroy(ejection.canopyObject.GetComponent<Rigidbody>());
            Destroy(ejection.canopyObject.GetComponent<FloatingOriginTransform>());

            ejection.canopyObject.transform.parent = canopyParent;
            ejection.canopyObject.transform.localPosition = canopyPos;
            ejection.canopyObject.transform.localRotation = canopyRot;
        }

        BlackoutEffect componentInChildren = VRHead.instance.GetComponentInChildren<BlackoutEffect>();
        if (componentInChildren)
        {
            componentInChildren.rb = GetComponent<Rigidbody>();
            componentInChildren.useFlightInfo = true;
        }
        ejection.gameObject.transform.parent = ejectorParent;
        ejection.transform.localPosition = ejectorSeatPos;
        ejection.transform.localRotation = ejectorSeatRot;

        Destroy(ejection.gameObject.GetComponent<FloatingOriginShifter>());
        Destroy(ejection.gameObject.GetComponent<FloatingOriginTransform>());
        ejection.seatRB.isKinematic = true;
        ejection.seatRB.interpolation = RigidbodyInterpolation.None;
        ejection.seatRB.collisionDetectionMode = CollisionDetectionMode.Discrete;

        shifter.enabled = true;
        AudioController.instance.AddExteriorOpening("eject", 0f);
    }

    void PutPlayerBackInAircraft()
    {
        detacher.cameraRig.transform.parent = target;
        detacher.cameraRig.transform.position = target.position;
        detacher.cameraRig.transform.rotation = target.rotation;

        detacher.pilotModel.SetActive(false);

        Destroy(detacher.cameraRig.GetComponent<FloatingOriginShifter>());
        Destroy(detacher.cameraRig.GetComponent<FloatingOriginTransform>());
        foreach (VRHandController vrhandController2 in VRHandController.controllers)
        {
            if (vrhandController2)
            {
                Destroy(vrhandController2.gameObject.GetComponent<VRTeleporter>());
            }
        }

        shifter.enabled = true;
    }

    void Eject()
    {
        health.Kill();
    }

    void Death()
    {
        repspawnTimer = StartCoroutine("RespawnTimer");
    }
}
