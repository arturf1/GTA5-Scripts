using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;


public class MeasureType : Script
{

    bool pointToggle;
    bool enabled;
    bool measure;
	Vector3 startPoint; 
    Vector3 endPoint;   

	public MeasureType()
    {
    	UI.Notify("Loaded MeasureType.cs");

        startPoint = Game.Player.Character.Position;
        endPoint =  Game.Player.Character.Position;

    	// attach time methods 
        Tick += OnTick;
        KeyUp += onKeyUp;
    }

	private void onKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.NumPad3)
        {
        	enabled = !enabled;
        }

        if (e.KeyCode == Keys.NumPad9)
        {
            pointToggle = !pointToggle;
        } 

        if (e.KeyCode == Keys.M)
        {
            measure = !measure;
        } 
     
    }

    void OnTick(object sender, EventArgs e)
    {
    	if (enabled)
    	{   
            if(measure)
            {
                World.DrawMarker(MarkerType.DebugSphere, startPoint, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Green);
                World.DrawMarker(MarkerType.DebugSphere, endPoint, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Black);
                UI.ShowSubtitle("Distance " + startPoint.DistanceTo(endPoint).ToString());    
            }
            else
            {
                if(pointToggle)
                {
                    World.DrawMarker(MarkerType.DebugSphere, Game.Player.Character.Position + 5f*GameplayCamera.Direction, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Green);
                    startPoint = Game.Player.Character.Position + 5f*GameplayCamera.Direction;
                    World.DrawMarker(MarkerType.DebugSphere, endPoint, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Black);
                }
                else
                {
                    World.DrawMarker(MarkerType.DebugSphere, Game.Player.Character.Position + 5f*GameplayCamera.Direction, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Black);
                    endPoint = Game.Player.Character.Position + 5f*GameplayCamera.Direction;
                    World.DrawMarker(MarkerType.DebugSphere, startPoint, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Green);
                }
            }            
		}
    }
}
