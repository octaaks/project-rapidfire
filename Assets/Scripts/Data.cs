using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Data : MonoBehaviour
{
    public static void SaveProfile (ProfileData t_profile)
    {
        try
        {
            string path = Application.persistentDataPath + "/profile.dt";
            if (File.Exists(path)) File.Delete(path);

            FileStream file = File.Create(path);

            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(file, t_profile);
            file.Close();

            Debug.Log("SAVED");
        }
        catch
        {
            Debug.Log("Error ck");
        }

    }

    public static ProfileData LoadProfile()
    {
        ProfileData ret = new ProfileData();
        try
        {
            string path = Application.persistentDataPath + "/profile.dt";
            if (File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                ret = (ProfileData)bf.Deserialize(file);

                //Debug.Log("LOADED");
            }
        }
        catch
        {
            Debug.Log("File not found!");
        }
        return ret;
    }
}
