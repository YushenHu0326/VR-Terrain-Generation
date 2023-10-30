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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WriteText(string text)
    {
        if (writer != null)
        {
            text += " Time step: ";
            text += Time.time.ToString();

            writer.WriteLine(text);
        }
    }

    public void WriteVector(Vector3 v)
    {
        if (writer != null)
        {
            string text = "(";
            text += v.x.ToString();
            text += ", ";
            text += v.y.ToString();
            text += ", ";
            text += v.z.ToString();
            text += ")";

            text += " Time step: ";
            text += Time.time.ToString();

            writer.WriteLine(text);
        }
    }

    public void StartWriting()
    {
        string path = Application.dataPath + "/record.txt";
        writer = new StreamWriter(path, true);
        writer.WriteLine("User " + userIndex.ToString() + ": ");
    }

    public void EndWriting()
    {
        if (writer != null)
        {
            writer.WriteLine("");
            writer.WriteLine("");
            writer.Close();
        }
    }
}
