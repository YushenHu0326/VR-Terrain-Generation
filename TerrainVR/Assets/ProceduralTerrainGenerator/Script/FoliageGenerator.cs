using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageGenerator : MonoBehaviour
{
    public int sampleNum = 1024;

    public GameObject foliage;

    List<GameObject> foliages;

    Terrain targetTerrain;

    bool refreshPaint;
    // Start is called before the first frame update
    void Start()
    {
        targetTerrain = GameObject.FindObjectOfType<Terrain>();

        foliages = new List<GameObject>();

        GenerateFoliage();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateFoliage()
    {
        refreshPaint = true;

        foreach (GameObject f in foliages)
        {
            Destroy(f);
        }

        foliages.Clear();

        StartCoroutine(GenerateFoliageCoroutine());
    }

    IEnumerator GenerateFoliageCoroutine()
    {
        refreshPaint = false;

        for (int i = 0; i < sampleNum; i++)
        {
            SpawnFoliage();

            if (refreshPaint)
                break;

            if (i % 100 == 0)
                yield return new WaitForEndOfFrame();
        }
    }

    void SpawnFoliage()
    {
        float x = Random.Range(targetTerrain.gameObject.transform.position.x, targetTerrain.gameObject.transform.position.x + targetTerrain.terrainData.size.x);
        float z = Random.Range(targetTerrain.gameObject.transform.position.z, targetTerrain.gameObject.transform.position.z + targetTerrain.terrainData.size.z);

        RaycastHit hit;

        if (Physics.Raycast(new Vector3(x, 500f, z), Vector3.down * 500f, out hit))
        {
            if (hit.normal.y > 0.8f)
            {
                Vector3 pos = hit.point;

                if (pos.y - targetTerrain.gameObject.transform.position.y < 75)
                {
                    if (Random.Range(0, (pos.y - targetTerrain.gameObject.transform.position.y) / 75f) < 0.5f)
                    {
                        GameObject f = Instantiate(foliage, pos, Quaternion.identity);
                        f.transform.Rotate(new Vector3(0, Random.Range(0, 360), 0), Space.World);
                        foliages.Add(f);
                    }
                }
            }
        }
    }
}
