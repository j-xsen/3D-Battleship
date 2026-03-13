using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class array_grid : MonoBehaviour
{

    public GameObject cube_Prefab;


    public int rows = 5;
    public int columns = 5;
    public int depth = 5;

    public int x_axis_pos = 0;
    public int y_axis_pos = 0;
    public int z_axis_pos = 0;


    //array to store initialized cubes
    private GameObject[,,] cubeArray;

    private void Start()
    {

        cubeArray = new GameObject[rows, columns, depth];



        GenerateCubes();

    }

    void GenerateCubes()
    {
        for (int i = 0; i < rows; i++) //x-axis 
        {
            for (int j = 0; j < columns; j++) //y-axis
            {
                for (int k = 0; k < depth; k++) //z-axis
                {
                    // Calculate the position for the current cube
                    Vector3 position = new Vector3(i + x_axis_pos, j + y_axis_pos, k + z_axis_pos);

                    // Instantiate the cube prefab at the calculated position
                    GameObject newCube = Instantiate(cube_Prefab, position, Quaternion.identity);

                    // Optional: Parent the new cube to the GameObject this script is on for organization
                    newCube.transform.parent = this.transform;

                    // Store the new cube in the array
                    cubeArray[i, j, k] = newCube;
                }
            }
        }
    }


}
