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

public class DataGen : Script
{
                                                                 
	bool draw = false;
	bool pauseGame = false; 
	bool enabled = false;
    bool enabledTraffic = false; 

    const int IMAGE_HEIGHT = 210;
    const int IMAGE_WIDTH = 280;
 
	List<Vector3> pointsToDraw  = new List<Vector3>(); 
	List<Vector3> pointsToDrawBlue  = new List<Vector3>();
 	
 	Vector3 startLineLeft;
 	Vector3 startLineRight;
 	Vector3 endLineLeft;
 	Vector3 endLineRight;

 	Vector3 rayEnd; 

    Prop stopObj;

 	List<Vehicle> traffic = new List<Vehicle>();
 	List<Ped> drivers = new List<Ped>();

 	int trialNum = 0;
 	bool doneWithTrials = false;
 	int weatherForTrial = 0; 
    int maxNumCars = 5; 
 	bool record = false;
 
 	int frame = 0;  

 	string dataDir = "G:\\test\\";
 	string fmtFrame = "00000000";
 	string fmtTrack = "000";

 	StreamWriter file;

    Vector3 startPos; 
    Vector3 endPos; 

    bool noStopObjTrail = false; 

    int track = 5;

 	/*
 	Stop object types
 	N - no stop object 
 	R - red light 
 	Y - yellow light 
 	X - rail road crossing
 	S - stop sign 
 	*/
 	string objectType = "S";

 	/*
 	Weather types
 	E - extra sunny 
 	R - raining 
 	O - overcast 
 	F - foggy 
 	T - thunder 
 	C - cloudy 
 	*/
 	string weather = "";

 	float distToObj = 70.0f;

 	private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

 	[DllImport("C:\\Windows\\System32\\user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("C:\\Windows\\System32\\user32.dll")]
    private static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);
	[DllImport("C:\\Windows\\System32\\user32.dll")]
	private static extern IntPtr ClientToScreen(IntPtr hWnd, ref Point point);

    public struct SOI
    {
        public int modelHash;
        public string name;
        public Vector3 FUL;
        public Vector3 BLR;

        public override bool Equals(object ob)
        {
            if( ob is SOI ) 
            {
                SOI c = (SOI) ob;
                return c.modelHash == this.modelHash;
            }
            else 
            {
                return false;
            }
        }
    }

    List<SOI> stopObjSOI  = new List<SOI>();


	public DataGen()
    {
    	UI.Notify("Loaded DataGen.cs");
        stopObjSOI = loadSOI("C:\\Users\\Artur\\Desktop\\test.txt");

    	// attach time methods 
        Tick += OnTick;
        KeyUp += onKeyUp;
    }

	private void onKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.NumPad7)
        {
		 	endLineLeft = rayEnd;
		}
		if (e.KeyCode == Keys.NumPad9)
        {
		 	endLineRight = rayEnd;
        }
        if (e.KeyCode == Keys.NumPad4)
        {
            startLineLeft = rayEnd;
        }
        if (e.KeyCode == Keys.NumPad6)
        {
		 	startLineRight = rayEnd;
		}
        if (e.KeyCode == Keys.NumPad5)
        {
            spawnStopObj(rayEnd);
        }

        if (e.KeyCode == Keys.NumPad1)
        {
        	enabled = !enabled;
            draw = !draw;
        }

        if (e.KeyCode == Keys.NumPad2)
        {
        	if (stopObjVisible(stopObj))
            {
                UI.Notify("Visible");
            }
            else
            {
                UI.Notify("Not Visible");
            }
        }

        if (e.KeyCode == Keys.NumPad3)
        {
        	trialNum = 0;
        	doneWithTrials = false;
        	DirectoryInfo di = Directory.CreateDirectory(dataDir + track.ToString(fmtTrack) + "\\");
        	dataDir = dataDir + track.ToString(fmtTrack) + "\\";
        	file = new StreamWriter(dataDir + track.ToString(fmtTrack) + ".csv", true);
            startPos = getRandLoc(startLineLeft,startLineRight);
            endPos = getRandLoc(endLineLeft,endLineRight);
        	runTrial(trialNum, startPos, endPos);
        }    
    }

    void OnTick(object sender, EventArgs e)
    {
    	if (enabled)
    	{
    		float maxRayDist = 100f;      
		    RaycastResult rayResult = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, maxRayDist, IntersectOptions.Everything);
		    World.DrawMarker(MarkerType.DebugSphere, rayResult.HitCoords, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Green);
	    	rayEnd = rayResult.HitCoords;
	    }

    	if (draw)
    	{  
    		int obj = 0; 
    		World.DrawMarker(MarkerType.DebugSphere, endLineLeft, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Red);
    		World.DrawMarker(MarkerType.DebugSphere, endLineRight, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Red);
    		World.DrawMarker(MarkerType.DebugSphere, startLineLeft, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Red);
    		World.DrawMarker(MarkerType.DebugSphere, startLineRight, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Red);

    		foreach (Vector3 v in pointsToDraw) 
    		{
    			World.DrawMarker(MarkerType.DebugSphere, v, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.05f,.05f,.05f), Color.Red);
    		} 

    		foreach (Vector3 v in pointsToDrawBlue) 
    		{
    			World.DrawMarker(MarkerType.DebugSphere, v, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Blue);
    		} 
		}

		List<Vehicle> vehForRemoval = new List<Vehicle>();
		List<Ped> pedForRemoval = new List<Ped>();
		int vehIndex = 0; 

		foreach (Vehicle v in traffic) 
		{
			Vector3 vehicleFront = v.Position + v.Model.GetDimensions().Y/2.0f*v.ForwardVector;
			vehicleFront.Z = vehicleFront.Z - (vehicleFront.Z - World.GetGroundHeight(vehicleFront));

			float distToEnd = Vector3.Distance2D(Vector3.Project(endLineLeft - vehicleFront, endLineLeft - endLineRight), endLineLeft - vehicleFront);
			
			if (!v.IsDriveable || v.IsSeatFree(VehicleSeat.Driver) || distToEnd < 2f) 
			{
				v.Delete();
				drivers[vehIndex].Delete();
				vehForRemoval.Add(v);
				pedForRemoval.Add(drivers[vehIndex]);
			}

			vehIndex = vehIndex + 1; 
		}

        // Remove here
		traffic.RemoveAll(x => vehForRemoval.Contains(x));
		drivers.RemoveAll(x => pedForRemoval.Contains(x));

        if (drivers.Count < maxNumCars && enabledTraffic)
        {
            addVehicle();
        }

		if (Game.Player.Character.IsInVehicle())
		{
			Vehicle v = Game.Player.Character.CurrentVehicle;
			Vector3 vehicleFront = v.Position + v.Model.GetDimensions().Y/2.0f*v.ForwardVector;
			vehicleFront.Z = vehicleFront.Z - (vehicleFront.Z - World.GetGroundHeight(vehicleFront));

			float distToEnd = Vector3.Distance2D(Vector3.Project(endLineLeft - vehicleFront, endLineLeft - endLineRight), endLineLeft - vehicleFront);
			
			if (distToEnd < 2f && !doneWithTrials) 
			{
				record = false;
                noStopObjTrail = !noStopObjTrail;
                if(noStopObjTrail)
                {
                    hideStopObj();
                    runTrial(trialNum, startPos, endPos);
                }
                else
                {
                    showStopObj();
                    startPos = getRandLoc(startLineLeft,startLineRight);
                    endPos = getRandLoc(endLineLeft,endLineRight);
                    trialNum = trialNum + 1;
                    runTrial(trialNum, startPos, endPos);
                }
            }
		}

		if (record)
    	{
    		if (weatherForTrial == 1)
    			weather = "E";
    		if (weatherForTrial == 2)
    			weather = "F";
    		if (weatherForTrial == 3)
    			weather = "O";
    		if (weatherForTrial == 4)
    			weather = "R";
    		if (weatherForTrial == 5)
    			weather = "T";
    		if (weatherForTrial == 6)
    			weather = "C";

            distToObj = Vector3.Distance(Vector3.Project(World.RenderingCamera.Position - endLineLeft, endLineLeft - endLineRight), World.RenderingCamera.Position - endLineLeft);
            
            if (distToObj > 70.0f || !stopObjVisible(stopObj) || objectType == "N" || noStopObjTrail)
            {
                distToObj = 70.0f;
                file.WriteLine(track.ToString(fmtTrack) + "-" + frame.ToString(fmtFrame) + ",N," + distToObj.ToString() + "," + (trialNum % 24).ToString() + "," + weather);
                screenshot(dataDir + track.ToString(fmtTrack) + "-" + frame.ToString(fmtFrame) + ".bmp");
                frame = frame + 1;
            }
            else
            {
                screenshot(dataDir + track.ToString(fmtTrack) + "-" + frame.ToString(fmtFrame) + ".bmp");
                file.WriteLine(track.ToString(fmtTrack) + "-" + frame.ToString(fmtFrame) + "," + objectType + "," + distToObj.ToString() + "," + (trialNum % 24).ToString() + "," + weather);
                frame = frame + 1;
            }

    	}
    }

    //////////////////////////////////////////////////////////////////////////////////
    //                        TRAIL FUNCTIONS 
    //////////////////////////////////////////////////////////////////////////////////
    void runTrial(int trial, Vector3 start, Vector3 end)
    {
    	World.CurrentDayTime = new TimeSpan(trial % 24, 0, 0);

    	if (trial % 24 == 0 && (noStopObjTrail == false))
    	{
            if (weatherForTrial == 6)
                weatherForTrial = 0;

    		weatherForTrial = weatherForTrial + 1;

    		if (weatherForTrial == 1)
    			World.TransitionToWeather(Weather.ExtraSunny, 10);
    		if (weatherForTrial == 2)
    			World.TransitionToWeather(Weather.Foggy, 10);
    		if (weatherForTrial == 3)
    			World.TransitionToWeather(Weather.Overcast, 10);
    		if (weatherForTrial == 4)
    			World.TransitionToWeather(Weather.Raining, 10);
    		if (weatherForTrial == 5)
    			World.TransitionToWeather(Weather.ThunderStorm, 10);
    		if (weatherForTrial == 6)
    			World.TransitionToWeather(Weather.Clearing, 10);		
    	}
    	

        Game.Player.Character.Task.ClearAll();
    	Vehicle v = Game.Player.Character.CurrentVehicle;
		v.Position = start;
		v.Heading = (endLineLeft - startLineLeft).ToHeading();
        Wait(1000);
		Game.Player.Character.Task.DriveTo(v, end, 2f, 10f, 786603);

		if (trial == 144)
		{
            record = false;
			doneWithTrials = true;
            file.Close();
            return;
		}

		record = true; 
		return;
    }

    Vector3 getRandLoc(Vector3 start, Vector3 end)
    {
        Random random = new Random();
        return (float)random.NextDouble()*(end - start) + start;
    }

    bool isSpaceFree(Vector3 point, Model carModel)
    {
        Vehicle[] v = World.GetNearbyVehicles(point, 1.3f*carModel.GetDimensions().Y);
        

        if (v.Length != 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////
    //                        STOP OBJECT FUNCTIONS 
    //////////////////////////////////////////////////////////////////////////////////
    // Function used to create a new stop sign
    public void spawnStopObj(Vector3 pos)
    {
        int n = -949234773;
        Model m = new Model((int)n); 
        Prop p = World.CreateProp(m, pos, Game.Player.Character.Rotation, false, true);
        stopObj = p; 
        showStopObj();
    }

    public void hideStopObj()
    {
        stopObj.IsVisible = false;
    }

    public void showStopObj()
    {
        stopObj.IsVisible = true;
    }

    public bool stopObjVisible(Prop stopObj)
    {
        bool visible = false;

        foreach (SOI s in stopObjSOI)
        {
            if(s.modelHash ==  stopObj.Model.Hash)
            {
                Vector3 FUL = s.FUL.X*stopObj.RightVector + s.FUL.Y*(Vector3.Cross(stopObj.UpVector, stopObj.RightVector)) + s.FUL.Z*stopObj.UpVector + stopObj.Position;
                Vector3 BLR = s.BLR.X*stopObj.RightVector + s.BLR.Y*(Vector3.Cross(stopObj.UpVector, stopObj.RightVector)) + s.BLR.Z*stopObj.UpVector + stopObj.Position;
                Vector3 dim = new Vector3();

                dim.X = -System.Math.Abs(s.FUL.X - s.BLR.X);
                dim.Y = System.Math.Abs(s.FUL.Y - s.BLR.Y);
                dim.Z = System.Math.Abs(s.FUL.Z - s.BLR.Z);

                if(visibleOnScreen(stopObj, dim, FUL, BLR))
                {
                    visible = true; 
                    break;
                }
            }
        }

        return visible;
    }


    private bool visibleOnScreen(Entity e, Vector3 dim, Vector3 FUL, Vector3 BLR)
    {
        bool isOnScreen = false; 

        Vector3[] vertices = new Vector3[8];

        vertices[0] = FUL;
        vertices[1] = FUL - dim.X*e.RightVector;
        vertices[2] = FUL - dim.Z*e.UpVector;
        vertices[3] = FUL - dim.Y*Vector3.Cross(e.UpVector, e.RightVector);

        vertices[4] = BLR;
        vertices[5] = BLR + dim.X*e.RightVector;
        vertices[6] = BLR + dim.Z*e.UpVector;
        vertices[7] = BLR + dim.Y*Vector3.Cross(e.UpVector, e.RightVector);


        foreach (Vector3 v in vertices)
        {
            if(UI.WorldToScreen(v).X != 0 && UI.WorldToScreen(v).Y != 0)
            {
                // is if point is visiable on screen
                Vector3 f = World.RenderingCamera.Position;
                Vector3 h = World.Raycast(f, v, IntersectOptions.Everything).HitCoords;

                if ((h-f).Length() < (v-f).Length())
                {
                    break;
                }
                else
                {
                    isOnScreen = true;
                    break;
                }
            }
        }

        return isOnScreen; 
    }

    private List<SOI> loadSOI(string filename)
    {
        System.IO.StreamReader file = new System.IO.StreamReader(filename);
        List<SOI> items  = new List<SOI>();
        string line;

        while((line = file.ReadLine()) != null)
        {
            SOI item = new SOI(); 
            string[] tokens = line.Split(' ');

            item.modelHash = Int32.Parse(tokens[0]); 
            item.name = tokens[1];
            item.FUL = new Vector3(float.Parse(tokens[2]),float.Parse(tokens[3]),float.Parse(tokens[4]));
            item.BLR = new Vector3(float.Parse(tokens[5]),float.Parse(tokens[6]),float.Parse(tokens[7]));

            items.Add(item);
        }
        file.Close();

        return items;
    } 

    
    //////////////////////////////////////////////////////////////////////////////////
    //                        TRAFFIC FUNCTIONS 
    //////////////////////////////////////////////////////////////////////////////////

    void addVehicle()
    {
        Random random = new Random();

        // find start point
        Vector3 start = getRandLoc(startLineLeft,startLineRight);

        // pick a random vehicle model 
        Array values = Enum.GetValues(typeof(VehicleHash));
        VehicleHash randomModel;
        do 
        {
            randomModel = (VehicleHash)values.GetValue(random.Next(values.Length));
        } while (randomModel == VehicleHash.ArmyTrailer
                || randomModel == VehicleHash.ArmyTanker 
                || randomModel == VehicleHash.ArmyTrailer2
                || randomModel == VehicleHash.BaleTrailer
                || randomModel == VehicleHash.BoatTrailer
                || randomModel == VehicleHash.DockTrailer
                || randomModel == VehicleHash.FreightTrailer
                || randomModel == VehicleHash.GrainTrailer
                || randomModel == VehicleHash.PropTrailer
                || randomModel == VehicleHash.RakeTrailer
                || randomModel == VehicleHash.TrailerLogs
                || randomModel == VehicleHash.Trailers
                || randomModel == VehicleHash.Trailers2
                || randomModel == VehicleHash.Trailers3
                || randomModel == VehicleHash.TrailerSmall
                || randomModel == VehicleHash.TVTrailer
                || randomModel == VehicleHash.Tanker
                || randomModel == VehicleHash.Tanker2
                || randomModel == VehicleHash.TRFlat
                || randomModel == VehicleHash.TR2
                || randomModel == VehicleHash.TR3
                || randomModel == VehicleHash.TR4);

        if (isSpaceFree(start, randomModel))
        {
            // create vehicle 
            Vehicle veh = World.CreateVehicle(randomModel,getRandLoc(startLineLeft,startLineRight), 0f);
            // make the vehicle face down the road 
            veh.Heading = (endLineLeft - startLineLeft).ToHeading();
            // pick a random color
            values = Enum.GetValues(typeof(VehicleColor));
            VehicleColor randomColor = (VehicleColor)values.GetValue(random.Next(values.Length));
            veh.PrimaryColor = randomColor;
            veh.IsInvincible = true;
            veh.CanBeVisiblyDamaged = false;

            Ped driver = veh.CreateRandomPedOnSeat(VehicleSeat.Driver);

            if (Game.Player.Character.IsInVehicle())
            {
                veh.SetNoCollision(Game.Player.Character.CurrentVehicle, true);
            }   

            if (veh.ClassType != VehicleClass.Boats 
            && veh.ClassType != VehicleClass.Helicopters
            && veh.ClassType != VehicleClass.Planes 
            && veh.ClassType != VehicleClass.Trains
            && veh.IsSeatFree(VehicleSeat.Driver) != true)
            {
                driver.Task.DriveTo(driver.CurrentVehicle, getRandLoc(endLineLeft,endLineRight), 2f, 10f, 786603);

                traffic.Add(veh);
                drivers.Add(driver);

                foreach (Vehicle v in traffic) 
                {
                    v.SetNoCollision(veh, true);
                }
            }
            else
            {
                veh.Delete();
                driver.Delete();
            }
        }

        return;
    }


    //////////////////////////////////////////////////////////////////////////////////
    //                        SCREENSCHOT FUNCTIONS 
    //////////////////////////////////////////////////////////////////////////////////
    void screenshot(String filename)
    {
        //UI.Notify("Taking screenshot?");

        var foregroundWindowsHandle = GetForegroundWindow();
        var rect = new Rect();
        GetClientRect(foregroundWindowsHandle, ref rect);

 		var pTL = new Point();
 		var pBR = new Point();
 		pTL.X = rect.Left; 
 		pTL.Y = rect.Top; 
 		pBR.X = rect.Right;
 		pBR.Y = rect.Bottom;

 		ClientToScreen(foregroundWindowsHandle, ref pTL);
		ClientToScreen(foregroundWindowsHandle, ref pBR);

        Rectangle bounds = new Rectangle(pTL.X, pTL.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);

        using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.ScaleTransform(.2f, .2f);
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }
            Bitmap output = new Bitmap(IMAGE_WIDTH, IMAGE_HEIGHT);
            using (Graphics g = Graphics.FromImage(output))
            {
                g.DrawImage(bitmap, 0, 0, IMAGE_WIDTH, IMAGE_HEIGHT);
            }
            output.Save(filename, ImageFormat.Bmp);

        }
    }

}