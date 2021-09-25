using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainScript : MonoBehaviour
{
    public Material mat;
    public ParticleSystem pSys;
    private TerrainData _terraindata;

    // Start is called before the first frame update
    void Start()
    {
        pSys.Stop();
        _terraindata = new TerrainData();
       
        _terraindata.SetDetailResolution(66, 66);
        GameObject terrain = Terrain.CreateTerrainGameObject(_terraindata);
        Terrain T = terrain.transform.GetComponent<Terrain>();
        T.materialTemplate = mat;
        terrain.transform.position = transform.position;

        float[,] heightmaps= new float [_terraindata.heightmapResolution, _terraindata.heightmapResolution];
        for (int i = 0; i < _terraindata.heightmapResolution; ++i)
        {
            
            for (int j = 0; j < _terraindata.heightmapResolution; ++j)
            {
                heightmaps[i, j] = Random.Range(0.5f,0.7f);
            }
        }
        _terraindata.SetHeightsDelayLOD(0,0, heightmaps);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator makeHole(int x, int y)
    {
        
        pSys.transform.position = new Vector3(x, 1, y );
        x += (int)Mathf.Abs(transform.position.x);
        y += (int)Mathf.Abs(transform.position.z);
        pSys.Play();
        for (float dist = 1f; dist  > 0.0f; dist -= (0.3f * Time.deltaTime))
        {
            _terraindata.SetHeightsDelayLOD(x, y, new float[,] { { dist } });
            yield return new WaitForEndOfFrame();
        }
        pSys.Stop();

    }
    public IEnumerator BulgeMe(int x, int y)
    {
        x += (int)Mathf.Abs(transform.position.x);
        y += (int)Mathf.Abs(transform.position.z);
        float startH = _terraindata.GetHeight(x, y);
        for (float dist = startH; dist < 1f; dist += (0.2f * Time.deltaTime))
        {

            _terraindata.SetHeightsDelayLOD(x, y, new float[,] { { dist } });
            yield return new WaitForEndOfFrame();
        }


    }


}
