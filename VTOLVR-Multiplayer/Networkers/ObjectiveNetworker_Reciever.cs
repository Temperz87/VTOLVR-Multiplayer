using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


class ObjectiveNetworker_Reciever
{
    private static MissionManager mManager = MissionManager.instance;
    public static Dictionary<int, VTEventTarget> scenarioActionsList = new Dictionary<int, VTEventTarget>();
    public static Dictionary<VTEventTarget,int> reverseScenarioActionsList = new Dictionary<VTEventTarget, int>();
    public static Dictionary<int, float> scenarioActionsListCoolDown = new Dictionary<int, float>();

    public static Dictionary<int, MissionObjective> objectiveHashTable = new Dictionary<int, MissionObjective>();
    public static Dictionary<MissionObjective, int> reverseObjectiveHashTable = new Dictionary<MissionObjective, int>();
    public static bool completeNext = false;
    public static bool completeNextEvent = false;
    public static bool completeNextFailed = false;
    public static bool completeNextBegin = false;
    public static bool completeNextCancel = false;
    public static MissionObjective[] objectivesList;
    public static List<VTObjective> VTobjectivesList;
    public static List<Message_ObjectiveSync> ObjectiveHistory = new List<Message_ObjectiveSync>();

    public static int getVTObjectiveHash(VTObjective VTobj)
    {
        string hashStr = VTobj.objectiveName + VTobj.objectiveInfo + VTobj.required;
        int hashCode = hashStr.GetHashCode();
        return hashCode;
    }
    public static int getMissionHash(MissionObjective obj)
    {
        string hashStr = obj.objectiveName + obj.info + obj.required;
        int hashCode = hashStr.GetHashCode();
        return hashCode;
    }
    public static void loadObjectives()
    {
        objectivesList = GameObject.FindObjectsOfType<MissionObjective>();

        foreach (var obj in objectivesList)
        {
            int hashCode = getMissionHash(obj);

            if (!objectiveHashTable.ContainsKey(hashCode))
            {
                objectiveHashTable[hashCode] = obj;
            }
            if (!reverseObjectiveHashTable.ContainsKey(obj))
            {
                reverseObjectiveHashTable[obj] = hashCode;
            }
        }
        Debug.Log($"compiled objective hashes");
    }
    public static void sendObjectiveHistory(CSteamID id)
    {
        if(Networker.isHost)
        foreach(var msg in ObjectiveHistory)
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(id, msg, Steamworks.EP2PSend.k_EP2PSendReliable);
        }
    }
    public static void objectiveUpdate(int hashCode, ObjSyncType status)
    {
        Debug.Log($"Doing objective update for id {hashCode}.");

        if(status == ObjSyncType.EVTBegin)
        {
            //VTScenario.current.objectives.GetObjective(hashCode).Dispose();
            VTScenario.current.objectives.GetObjective(hashCode).BeginObjective();
        }

        if (!objectiveHashTable.ContainsKey(hashCode))
        {
            loadObjectives();
        }

        if (!objectiveHashTable.ContainsKey(hashCode))
        {
            Debug.Log("cant find objective in hashTable");
            return;
        }
      

        MissionObjective obj = objectiveHashTable[hashCode];
        if (obj == null)
        {
            Debug.Log("obj was Null");
            return;
        }


        if (status == ObjSyncType.EMissionCompleted && !obj.completed)
        {
            Debug.Log("Completeing mission complete locally");
            completeNext = true;
            if (!obj.started)
            {
                obj.BeginMission();
            }
            obj.CompleteObjective();
            loadObjectives();
        }

        if (status == ObjSyncType.EMissionFailed && !obj.failed)
        {
            Debug.Log("failing mission complete locally");
            completeNextFailed = true;
            if (!obj.started)
                obj.BeginMission();

            obj.FailObjective();
            loadObjectives();
        }

        if (status == ObjSyncType.EMissionBegin && !obj.started)
        {
            Debug.Log("starting mission begin locally");
            completeNextBegin = true;
            obj.BeginMission();
            loadObjectives();
        }

        if (status == ObjSyncType.EMissionCanceled && !obj.cancelled)
        {
            Debug.Log("starting mission cancel locally");
            completeNextCancel = true;
            if (!obj.started)
                obj.BeginMission();
            obj.CancelObjective();
            loadObjectives();
        }
    }
    public static void runScenarioAction(int hash)
    {
        if (scenarioActionsListCoolDown.ContainsKey(hash))
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - scenarioActionsListCoolDown[hash] > 5.0f)
            {
                if (scenarioActionsList.ContainsKey(hash))
                {

                    scenarioActionsListCoolDown.Remove(hash);
                    scenarioActionsListCoolDown.Add(hash, currentTime);
                    completeNextEvent = true;
                    scenarioActionsList[hash].Invoke();
                }
                else
                    Debug.Log("scenario error doesnt exsist");
            }
        }
        else

        {

            if (scenarioActionsList.ContainsKey(hash))
            {
                float currentTime = Time.unscaledTime;
                scenarioActionsList[hash].Invoke();
                completeNextEvent = true;
                scenarioActionsListCoolDown.Add(hash, currentTime);

            }
            else
                Debug.Log("secanrio error doesnt exsist");
        }

    }

    public static void cleanUp()
    {

        scenarioActionsList.Clear();
     reverseScenarioActionsList.Clear();
        scenarioActionsListCoolDown.Clear();

      objectiveHashTable.Clear();
     reverseObjectiveHashTable.Clear();
     completeNext = false;
     completeNextEvent = false;
     completeNextFailed = false;
     completeNextBegin = false;
     completeNextCancel = false;
        ObjectiveHistory.Clear();
    }
}
