using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class UserStudy : MonoBehaviour
{
    StreamWriter writer;

    public int userIndex;

    // Start is called before the first frame update
    void Start()
    {
        string path = Application.dataPath + "/UserStudy/record.txt";
        writer = new StreamWriter(path, true);
    }

    void OnApplicationExit()
    {
        writer.Close();
    }

    public void Write(int modification, int handIndex, Vector3 v)
    {
        if (writer != null)
        {
            string text = "";
            text += userIndex.ToString();
            text += " ";
            text += modification.ToString();
            text += " ";
            text += handIndex.ToString();
            text += " ";
            text += v.x.ToString();
            text += " ";
            text += v.y.ToString();
            text += " ";
            text += v.z.ToString();
            text += " ";
            text += Time.time.ToString();

            writer.WriteLine(text);
        }
    }
}
