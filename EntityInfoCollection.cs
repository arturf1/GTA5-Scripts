/*
Authors: Artur Filipowicz
Version: 0.9
Copyright: 2016
MIT License
*/

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class EntityInfoCollection : Script
{

	bool enabled = false;
	Entity[] entities;
	float maxRayDist = 100f; 

	Vector3 FUL; // forward upper left corner 
	Vector3 BLR; // back lower right
	UIContainer mContainer;

	public EntityInfoCollection()
    {
    	UI.Notify("Loaded EntityInfoCollection.cs");

    	BLR = Vector3.Zero; 
    	FUL = Vector3.Zero; 

    	// attach time methods 
        Tick += OnTick;
        KeyUp += onKeyUp;
    }

	private void onKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.NumPad1)
        {
        	collectEntityData();
        	enabled = !enabled;
        } 

        /*if (e.KeyCode == Keys.B)
        {
        	collectEntityData(); 
        } */

        if (e.KeyCode == Keys.N)
        {
        	delEntity();
        }
     
    }

    void OnTick(object sender, EventArgs e)
    {
    	if (enabled)
    	{             
		    RaycastResult rayResult = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, maxRayDist, IntersectOptions.Everything);
		    World.DrawMarker(MarkerType.DebugSphere, rayResult.HitCoords, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Green);
	            	
			foreach (Entity entity in entities)
	        {
	            if (entity.IsOnScreen)
	            {
 					World.DrawMarker(MarkerType.DebugSphere, entity.Position, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Blue);
	            }
	        }

	        World.DrawMarker(MarkerType.DebugSphere, FUL, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.1f,.1f,.1f), Color.Black);
	        World.DrawMarker(MarkerType.DebugSphere, BLR, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.1f,.1f,.1f), Color.Black);
	     	   
		}
    }

    private void collectEntityData()
    {
        entities = World.GetAllEntities();
    }

	private void inspectEntity()
	{
		RaycastResult rayResult = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, maxRayDist, IntersectOptions.Everything);
		Entity[] nearEntities = World.GetNearbyEntities(rayResult.HitCoords, .3f);


		if (nearEntities.Length > 0)
		{
			Model m = nearEntities[0].Model;
			Vector3 dim = m.GetDimensions();

			UI.ShowSubtitle(m.Hash.ToString() + " h: " + dim.Z.ToString() + " w: " + dim.X.ToString() + " z: " + dim.Y.ToString());
			
			float offset = 1.9f; 
			FUL = - (dim.X/2f)*nearEntities[0].RightVector + dim.Z*nearEntities[0].UpVector;
			BLR = (dim.X/2f)*nearEntities[0].RightVector + offset*nearEntities[0].UpVector;

			System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Artur\\Desktop\\test.txt");
			file.WriteLine(m.Hash.ToString() + " " + (-dim.X/2f).ToString() + " " + 0 + " " + dim.Z.ToString() + " " + (dim.X/2f).ToString() + " " + 0 + " " + offset.ToString());
			file.Close();

			FUL = FUL + nearEntities[0].Position;
			BLR = BLR + nearEntities[0].Position;

			int width = UI.WorldToScreen(FUL).X - UI.WorldToScreen(BLR).X;
			int height = UI.WorldToScreen(BLR).Y - UI.WorldToScreen(FUL).Y;
			this.mContainer = new UIContainer(new Point(UI.WorldToScreen(FUL).X - width, UI.WorldToScreen(FUL).Y), new Size(width, height), Color.FromArgb(200, 237, 239, 241));
        	this.mContainer.Draw();

		}
	}

	private void delEntity()
	{
		RaycastResult rayResult = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, maxRayDist, IntersectOptions.Everything);
		Entity[] nearEntities = World.GetNearbyEntities(rayResult.HitCoords, .3f);


		if (nearEntities.Length > 0)
		{
			nearEntities[0].Delete();
		}
	}
}




