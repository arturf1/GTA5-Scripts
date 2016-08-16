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

public class Annotator : Script
{
   
	// Maximum distance an entity can be from the camera 
	float maxAnnotationRange = 70f; 
                                                                 
	bool draw = false;
	bool pauseGame = false; 


    const int IMAGE_HEIGHT = 600;
    const int IMAGE_WIDTH = 800;

    List<string> objectInfo  = new List<string>();  
	List<Vector3> frontUpperLeft  = new List<Vector3>(); 
	List<Vector3> backLowerRight  = new List<Vector3>();
	List<Vector3> pointsToDraw  = new List<Vector3>(); 
	List<Vector3> pointsToDrawBlue  = new List<Vector3>();
	List<Vector3> roadNodes  = new List<Vector3>();

	Entity[] entities;
 	
 	UIContainer mContainer;

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

	List<SOI> trafficSigns  = new List<SOI>();

	public Annotator()
    {
    	UI.Notify("Loaded Annotator.cs");

    	trafficSigns = loadSOI("C:\\Users\\Artur\\Desktop\\test.txt");

    	// attach time methods 
        Tick += OnTick;
        KeyUp += onKeyUp;
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

	private void onKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.NumPad7)
        {
        	pauseGame =!pauseGame;
        	Game.Pause(pauseGame);
        	annotateEntities();
        	pointsToDrawBlue.Add(10f*Game.Player.Character.CurrentVehicle.ForwardVector + Game.Player.Character.CurrentVehicle.Position);
        	draw = !draw;
        	pauseGame =!pauseGame;
        	Game.Pause(pauseGame);
        } 

        if (e.KeyCode == Keys.N)
        {
        	draw = !draw;
        	markLanes(); 
        }     
    }

    void OnTick(object sender, EventArgs e)
    {
    	if (draw)
    	{  
    		int obj = 0; 

    		foreach (Vector3 ul in frontUpperLeft)
    		{
				World.DrawMarker(MarkerType.DebugSphere, ul, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.1f,.1f,.1f), Color.Black);
	        	World.DrawMarker(MarkerType.DebugSphere, backLowerRight[obj], new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.1f,.1f,.1f), Color.Black);
    			obj = obj + 1; 
    		} 

    		foreach (Vector3 v in pointsToDraw) 
    		{
    			World.DrawMarker(MarkerType.DebugSphere, v, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Red);
    		} 

    		foreach (Vector3 v in pointsToDrawBlue) 
    		{
    			World.DrawMarker(MarkerType.DebugSphere, v, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Blue);
    		} 

    		/*foreach (Vector3 v in roadNodes) 
    		{
    			World.DrawMarker(MarkerType.DebugSphere, v, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Red);
    		} */
		}
    }

    private void annotateEntities()
    {
    	objectInfo.Clear();  
		frontUpperLeft.Clear(); 
		backLowerRight.Clear();

        entities = World.GetNearbyEntities(GameplayCamera.Position, maxAnnotationRange);

        foreach (Entity entity in entities)
        {
            if (entity.IsOnScreen && !entity.IsOccluded)
            {
            	World.DrawMarker(MarkerType.DebugSphere, entity.Position, new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(.3f,.3f,.3f), Color.Blue);
	            
	            Vector3 front = Vector3.Cross(entity.UpVector, entity.RightVector);
	            Vector3 camDir = GameplayCamera.Direction; 
	            float ang = Vector3.Dot(front, camDir)/(front.LengthSquared()*camDir.LengthSquared());

	            if (ang > 0)
	            {
	            	Model m = entity.Model;
					annotObject(entity,m);
				}
            }
        }

        Vehicle[] vehicles = World.GetNearbyVehicles(GameplayCamera.Position, maxAnnotationRange);

        foreach (Vehicle v in vehicles)
        {

        	if (v != Game.Player.Character.CurrentVehicle)
            {
            	Vector3 dim = v.Model.GetDimensions();

            	//UI.Notify(v.HeightAboveGround.ToString());

            	Vector3 FUL = v.GetOffsetInWorldCoords(new Vector3(dim.X/2f, dim.Y/2f, dim.Z/2f));
            	FUL.Z = FUL.Z - (FUL.Z - World.GetGroundHeight(FUL)) + dim.Z;
				Vector3 BLR = v.GetOffsetInWorldCoords(new Vector3(-dim.X/2f, -dim.Y/2f , -dim.Z/2f));
				BLR.Z = BLR.Z - (BLR.Z - World.GetGroundHeight(BLR));

				//UI.Notify("CCC " + World.GetGroundHeight(BLR).ToString());

				if(visibleOnScreen(v, FUL, BLR))
				{
					drawModelEnds(FUL, BLR);
            		drawBoundingBox(v, v.Model.GetDimensions(), FUL, BLR);
				}
            }
        }

        Ped[] peds = World.GetNearbyPeds(GameplayCamera.Position, maxAnnotationRange);

        foreach (Ped p in peds)
        {
			if (!p.IsInVehicle() && (p != Game.Player.Character))
            {          	
            	Vector3 dim = p.Model.GetDimensions();
            	Vector3 FUL = p.GetOffsetInWorldCoords(new Vector3(dim.X/2f, dim.Y/2f, dim.Z/2f));
				Vector3 BLR = p.GetOffsetInWorldCoords(new Vector3(-dim.X/2f, -dim.Y/2f , -dim.Z/2f));
				BLR.Z = BLR.Z - (BLR.Z - World.GetGroundHeight(BLR));
				FUL.Z = BLR.Z + dim.Z;

				UI.Notify("Dim " + dim.X.ToString() + " " + dim.Y.ToString() + " " + dim.Z.ToString());

				if(visibleOnScreen(p, FUL, BLR))
				{
	            	drawModelEnds(FUL, BLR);
	            	drawBoundingBox(p, p.Model.GetDimensions(), FUL, BLR);
	            }
            }
        }

        screenshot("C:\\Users\\Artur\\Desktop\\img.jpg");
    }

    private bool visibleOnScreen(Entity e, Vector3 FUL, Vector3 BLR)
    {
    	bool isOnScreen = false; 

    	Vector3[] vertices = new Vector3[8];
    	Vector3 dim = e.Model.GetDimensions();

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


    private void annotObject(Entity e, Model m)
    {
		foreach (SOI s in trafficSigns)
		{
			if(s.modelHash ==  m.Hash)
			{
				Vector3 FUL = s.FUL.X*e.RightVector + s.FUL.Y*(Vector3.Cross(e.UpVector, e.RightVector)) + s.FUL.Z*e.UpVector + e.Position;
				Vector3 BLR = s.BLR.X*e.RightVector + s.BLR.Y*(Vector3.Cross(e.UpVector, e.RightVector)) + s.BLR.Z*e.UpVector + e.Position;
				Vector3 dim = new Vector3();

				dim.X = -System.Math.Abs(s.FUL.X - s.BLR.X);
				dim.Y = System.Math.Abs(s.FUL.Y - s.BLR.Y);
				dim.Z = System.Math.Abs(s.FUL.Z - s.BLR.Z);

				UI.Notify("dim " + dim.X.ToString() + " " + dim.Y.ToString() + " " + dim.Z.ToString());

				drawModelEnds(FUL, BLR);
				drawBoundingBox(e, dim, FUL, BLR);
			}
		}
    }

    private void drawModelEnds(Vector3 FUL, Vector3 BLR)
    {
    	frontUpperLeft.Add(FUL);
		backLowerRight.Add(BLR);
    }

    private void drawBoundingBox(Entity e, Vector3 dim, Vector3 FUL, Vector3 BLR)
    {
    	Vector3[] vertices = new Vector3[8];
    	 
    	vertices[0] = FUL;
    	vertices[1] = FUL - dim.X*e.RightVector;
    	vertices[2] = FUL - dim.Z*e.UpVector;
    	vertices[3] = FUL - dim.Y*Vector3.Cross(e.UpVector, e.RightVector);

    	vertices[4] = BLR;
    	vertices[5] = BLR + dim.X*e.RightVector;
    	vertices[6] = BLR + dim.Z*e.UpVector;
    	vertices[7] = BLR + dim.Y*Vector3.Cross(e.UpVector, e.RightVector);

    	int xMin = int.MaxValue;
    	int yMin = int.MaxValue; 
    	int xMax = 0;
    	int yMax = 0; 

    	foreach (Vector3 v in vertices)
		{

			int x = (int)get2Dfrom3D(v).X;
			int y = (int)get2Dfrom3D(v).Y;

			//UI.Notify("Original " + UI.WorldToScreen(v).X.ToString() + " " + UI.WorldToScreen(v).Y.ToString());
			//UI.Notify("Mine " + x.ToString() + " " + y.ToString());

			if (x < xMin)
				xMin = x;
			if (x > xMax)
				xMax = x;
			if (y < yMin)
				yMin = y;
			if (y > yMax)
				yMax = y;
		} 	

		//UI.Notify(indPointOffView[0].ToString() + " " + indPointOffView[1].ToString() + " " + indPointOffView[2].ToString() + " " + indPointOffView[3].ToString() + " " + indPointOffView[4].ToString() + " " + indPointOffView[5].ToString() + " " + indPointOffView[6].ToString() + " " + indPointOffView[7].ToString());

		if (xMin < 0)
			xMin = 0;
		if (yMin < 0)
			yMin = 0;

		if (xMax > UI.WIDTH)
			xMax = UI.WIDTH;
		if (yMax > UI.HEIGHT)
			yMax = UI.HEIGHT;			

    	int width = xMax - xMin;
		int height = yMax - yMin;

    	xMin = (int)(IMAGE_WIDTH/(1.0*UI.WIDTH)*xMin);
		xMax = (int)(IMAGE_WIDTH/(1.0*UI.WIDTH)*xMax);
		yMin = (int)(IMAGE_HEIGHT/(1.0*UI.HEIGHT)*yMin);
		yMax = (int)(IMAGE_HEIGHT/(1.0*UI.HEIGHT)*yMax);

		width = xMax - xMin;
		height = yMax - yMin;

    	System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Artur\\Desktop\\IMG.txt", true);
		file.WriteLine(xMin.ToString() + " " + yMin.ToString() + " " + width.ToString() + " " + height.ToString());
		file.Close();
    }

    Vector2 get2Dfrom3D(Vector3 a)
    {
    	// camera rotation 
		Vector3 theta = (float)(System.Math.PI/180f)*World.RenderingCamera.Rotation;
    	// camera direction, at 0 rotation the camera looks down the postive Y axis 
    	Vector3 camDir = rotate(Vector3.WorldNorth, theta);

		//UI.Notify("camDir: " + camDir.X.ToString() + " " + camDir.Y.ToString() + " " + camDir.Z.ToString());
		

    	// camera position 
		Vector3 c = World.RenderingCamera.Position + World.RenderingCamera.NearClip*camDir;
		// viewer position 
		Vector3 e = -World.RenderingCamera.NearClip*camDir;
		// point locatios with repect to camera coordinates 
		Vector3 d; 

		float viewWindowHeight = 2*World.RenderingCamera.NearClip*(float)System.Math.Tan((World.RenderingCamera.FieldOfView/2f)*(System.Math.PI/180f));
		float viewWindowWidth = (IMAGE_WIDTH/((float)IMAGE_HEIGHT)) * viewWindowHeight;

		Vector3 camUp = rotate(Vector3.WorldUp, theta);
		Vector3 camEast = rotate(Vector3.WorldEast, theta);

		//UI.Notify("c: " + c.X.ToString() + " " + c.Y.ToString() + " " + c.Z.ToString());
		//UI.Notify("o: " + (c + (viewWindowHeight/2f)*camUp - (viewWindowWidth/2f)*camEast).X.ToString() + " " + (c + (viewWindowHeight/2f)*camUp - (viewWindowWidth/2f)*camEast).Y.ToString() + " " + (c + (viewWindowHeight/2f)*camUp - (viewWindowWidth/2f)*camEast).Z.ToString());

		//pointsToDrawBlue.Add(c + (viewWindowHeight/2f)*camUp - (viewWindowWidth/2f)*camEast);
		//intsToDrawBlue.Add(c + (viewWindowHeight/2f)*camUp + (viewWindowWidth/2f)*camEast);
		//intsToDrawBlue.Add(c - (viewWindowHeight/2f)*camUp - (viewWindowWidth/2f)*camEast);
		//intsToDrawBlue.Add(c - (viewWindowHeight/2f)*camUp + (viewWindowWidth/2f)*camEast);

		//pointsToDrawBlue.Add(c);
		
		Vector3 del = a - c;

		pointsToDraw.Add(del + c);

		//UI.Notify("a: " + a.X.ToString() + " " + a.Y.ToString() + " " + a.Z.ToString());
		//UI.Notify("d + c: " + (d + c).X.ToString() + " " + (d + c).Y.ToString() + " " + (d + c).Z.ToString());
 
 		/*for (int i = 1; i < 5; i++)
        {
        	pointsToDraw.Add((i/5f)*d + c);
        }*/

        //for (int i = 1; i < 10; i++)
        //{
        //	pointsToDrawBlue.Add((i/10f)*(d-e) + (c+e));
        //}

		Vector3 viewerDist = del-e;
		Vector3 viewerDistNorm = viewerDist*(1/viewerDist.Length());
		float dot = Vector3.Dot(camDir, viewerDistNorm);
		float ang = (float)System.Math.Acos((double)dot);
		float viewPlaneDist = World.RenderingCamera.NearClip/(float)System.Math.Cos((double)ang);
		Vector3 viewPlanePoint = viewPlaneDist*viewerDistNorm + e;

		//pointsToDrawBlue.Add(e + c);

		// move origin to upper left 
		Vector3 newOrigin = c + (viewWindowHeight/2f)*camUp - (viewWindowWidth/2f)*camEast;
		viewPlanePoint = (viewPlanePoint + c) - newOrigin;
		//pointsToDraw.Add(newOrigin + viewPlanePoint);

		//pointsToDraw.Add(newOrigin);
		//UI.Notify("newOrigin: " + newOrigin.X.ToString() + " " + newOrigin.Y.ToString() + " " + newOrigin.Z.ToString());
		//UI.Notify("viewPlanePoint: " + viewPlanePoint.X.ToString() + " " + viewPlanePoint.Y.ToString() + " " + viewPlanePoint.Z.ToString());

		float viewPlaneX = Vector3.Dot(viewPlanePoint, camEast) / Vector3.Dot(camEast, camEast);
		float viewPlaneZ = Vector3.Dot(viewPlanePoint, camUp) / Vector3.Dot(camUp, camUp);

		//pointsToDraw.Add(newOrigin + viewPlaneX*camEast + viewPlaneZ*camUp);

		float screenX = viewPlaneX/viewWindowWidth*UI.WIDTH; 
		float screenY = -viewPlaneZ/viewWindowHeight*UI.HEIGHT; 

    	return new Vector2((int)screenX, (int)screenY);
    }

    Vector3 rotate(Vector3 a, Vector3 theta)
    {
    	Vector3 d = new Vector3();

    	d.X = (float)System.Math.Cos((double)theta.Z)*((float)System.Math.Cos((double)theta.Y)*a.X + (float)System.Math.Sin((double)theta.Y)*((float)System.Math.Sin((double)theta.X)*a.Y + (float)System.Math.Cos((double)theta.X)*a.Z)) - (float)System.Math.Sin((double)theta.Z)*((float)System.Math.Cos((double)theta.X)*a.Y - (float)System.Math.Sin((double)theta.X)*a.Z);
		d.Y = (float)System.Math.Sin((double)theta.Z)*((float)System.Math.Cos((double)theta.Y)*a.X + (float)System.Math.Sin((double)theta.Y)*((float)System.Math.Sin((double)theta.X)*a.Y + (float)System.Math.Cos((double)theta.X)*a.Z)) + (float)System.Math.Cos((double)theta.Z)*((float)System.Math.Cos((double)theta.X)*a.Y - (float)System.Math.Sin((double)theta.X)*a.Z);
		d.Z = -(float)System.Math.Sin((double)theta.Y)*a.X + (float)System.Math.Cos((double)theta.Y)*((float)System.Math.Sin((double)theta.X)*a.Y + (float)System.Math.Cos((double)theta.X)*a.Z); 

		return d;
    }

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


    private void markLanes()
    {
    	Vector3 playerPos = Game.Player.Character.Position;

    	OutputArgument safe1 = new OutputArgument();
        OutputArgument safe2 = new OutputArgument();
        OutputArgument safe3 = new OutputArgument();
    	for (int i = 0; i < 25; i++)
        {
            Vector3 midNode;
            OutputArgument outPosArg = new OutputArgument();
            Function.Call(Hash.GET_NTH_CLOSEST_VEHICLE_NODE, playerPos.X, playerPos.Y, playerPos.Z, i, outPosArg, safe1, safe2, safe3);
            midNode = outPosArg.GetResult<Vector3>();
            roadNodes.Add(midNode);
            pointsToDraw.Add(midNode);

            
            if (i ==1)
            {
            	int nodeId = Function.Call<int>(Hash.GET_NTH_CLOSEST_VEHICLE_NODE_ID, playerPos.X, playerPos.Y, playerPos.Z, i, safe1, 0f, 0f);
            	UI.Notify("Node " + nodeId.ToString() + " " + World.GetStreetName(playerPos));

	            Vector3 roadPerp = Vector3.Cross(Vector3.WorldUp, Game.Player.Character.CurrentVehicle.ForwardVector);
	            Vector3 roadPerpNorm = (roadPerp)*(1/roadPerp.Length());
	            string streetName = World.GetStreetName(midNode); 
	            float laneWidth = 5.6f; 

	            // check if midNode is in middle of lane or on lane marking 

	            int numLanesMid = 0; 
	            int numLanesMark = 0; 

	            // assume midNode is in the middle of a lane 

	           	Vector3 firstLaneLeft = midNode + 0.5f*laneWidth*roadPerpNorm;
	           	Vector3 firstLaneRight = midNode - 0.5f*laneWidth*roadPerpNorm;

	           	pointsToDrawBlue.Add(firstLaneLeft);
	           	pointsToDrawBlue.Add(firstLaneRight);

	           	UI.Notify("L " + pointOnRoad(firstLaneLeft).ToString() + " " + World.GetStreetName(firstLaneLeft));
	           	UI.Notify("R " + pointOnRoad(firstLaneRight).ToString() + " " + World.GetStreetName(firstLaneRight));

	            if (String.Compare(streetName, World.GetStreetName(firstLaneLeft), true) == 0)
	            {
	            	if (String.Compare(streetName, World.GetStreetName(firstLaneRight), true) == 0)
		            {
		            	numLanesMid = numLanesMid + 1; 
		            }
	            }

	            // count number of lanes on the left 
	            int lanesOnLeft = 0;

	            pointsToDrawBlue.Add(midNode + (1.5f+lanesOnLeft)*laneWidth*roadPerpNorm);
	            UI.Notify("L1 " + World.GetStreetName(midNode + (1.5f+lanesOnLeft)*laneWidth*roadPerpNorm).ToString() + " " + World.GetStreetName((1.5f+lanesOnLeft)*laneWidth*roadPerpNorm));
	            
	            UI.Notify("L1 " + streetName.ToString() + " " + World.GetStreetName(midNode + (1.5f+lanesOnLeft)*laneWidth*roadPerpNorm));

	            while(String.Compare(streetName, World.GetStreetName(midNode + (1.5f+lanesOnLeft)*laneWidth*roadPerpNorm), true) == 0) 
	            {
	            	lanesOnLeft = lanesOnLeft + 1;
	            } 

	            // count number of lanes on the right
	            int lanesOnRight = 0; 
	            while(String.Compare(streetName, World.GetStreetName(midNode + (1.5f-lanesOnRight)*laneWidth*roadPerpNorm), true) == 0) 
	            {
	            	lanesOnRight = lanesOnRight + 1;
	            }

	            numLanesMid = numLanesMid + lanesOnRight + lanesOnLeft;

	            UI.Notify("lanesOnRight " + lanesOnRight.ToString() + " lanesOnLeft " + lanesOnLeft.ToString());

	            // assume midNode is on a lane marking
	           	Vector3 firstLeftLane = midNode + laneWidth*roadPerpNorm;
	            if (pointOnRoad(firstLeftLane) == true && World.GetStreetName(firstLeftLane) == streetName)
	            {
	            	numLanesMark = numLanesMark + 1; 
	            }

	            Vector3 firstRightLane = midNode - laneWidth*roadPerpNorm;
	            if (pointOnRoad(firstRightLane) == true && World.GetStreetName(firstRightLane) == streetName)
	            {
	            	numLanesMark = numLanesMark + 1; 
	            }

	            // count number of lanes on the left 
	            lanesOnLeft = 0;
	            while(pointOnRoad(midNode + (1f+lanesOnLeft)*laneWidth*roadPerpNorm) == true && World.GetStreetName(midNode + (1f+lanesOnLeft)*laneWidth*roadPerpNorm) == streetName) 
	            {
	            	lanesOnLeft = lanesOnLeft + 1;
	            } 

	            // count number of lanes on the right
	            lanesOnRight = 0; 
	            while(pointOnRoad(midNode - (1f+lanesOnRight)*laneWidth*roadPerpNorm) == true && World.GetStreetName(midNode - (1f+lanesOnRight)*laneWidth*roadPerpNorm) == streetName) 
	            {
	            	lanesOnRight = lanesOnRight + 1;
	            }

	            numLanesMark = numLanesMark + lanesOnRight + lanesOnLeft;
	            
	            UI.Notify("numLanesMark " + numLanesMark.ToString() + " numLanesMid " + numLanesMid.ToString());

	            
	        }
        }
    }

    private bool pointOnRoad(Vector3 v)
    {
    	return Function.Call<bool>(Hash.IS_POINT_ON_ROAD, v.X, v.Y, v.Z, 1f);
    }
}