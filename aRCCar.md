# Introduction

There is a lot of interest in AR (Augmented Reality) lately, partially due to Apple incorporating AR capabilities natively in the iOS operating system through the ARKit libraries. Shortly after Apple introduced ARKit, Google introduced ARCode which does basically the same thing.


So, what is that “same thing”? Well, they allow you to watch the world through your smartphone camera and place 3D content which interacts with the world shown. The video of the WWDC keynote, available here (https://developer.apple.com/videos/play/wwdc2017/602/ ), where the technology got introduced, shows a very nice example of what is possible.


This article will introduce the basic steps necessary to create such an application, a small game, using ARKit and Xamarin.


# Basics


ARKit uses a combination of the video from the camera and the phones motion sensors to identify the position of the phone in the world space. This technique is known as visual-inertial odometry.


Basically, through image processing techniques, ARKit finds features in the scene and tracks those features in the video feed. It then uses the motion sensors of the phone to correct this tracking. By tracking the features, ARKit is capable of forming itself an image of what the 3D world around it looks like. And using this information it can find surfaces in the 3D world. It then exposes these surfaces through the ARKit API allowing developers to place content in the 3D world viewed through the camera. To place something on this surface, you find the intersection of the "line of sight", coming out of the phones camera, and this plane.


## Prepare

== opzetten van een scene om de 3D content te kunnen tonen op een correcte manier in de 3D wereld.==


Standard 3D scenes on the iOS platform can be created using the SCNView class.
This view is part of the Apple SceneKit library which provides an API to manipulate and render 3D assets.


However, with Augmented Reality, there is an additional challenge: you want to place the 3D assets with reference to the 3D world viewed in/through your camera.


To be able to display content in the camera view, Apple provides the ARSCNView class. It takes care of the mapping of coordinates from the 3D world to positions on the 2D screen. The ARSCNView uses an ARSession objects to calculate this mapping. The ARSession class can be seen as the very heart of the AR experience. This function of this class is to integrate the various sensor readings (camera and motion) and to calculate a correspondence between the actual physical world and the virtual space the application uses.


You can have different types of AR experiences. You choose a certain type of experience through the type of ARConfiguration you provide to the ARSession. The type we'll use here is implemented through the ARWorldTrackingConfiguration class: it provides a 6DOF (degrees of freedom) experience viewed through the back camera.


==MIJN VRAAG: wat is dan het verschil tussen ARSession en ARWorldTrackingConfiguration?==


Following are the steps taken to place content on a plane found by ARKit:
<ol>
<li>Finding a point on the plane found by ARKit. There are a few possibilities here: one could allow theh usert to tap on the screen to specify a point. However, we'll be using the line of sight of the user to specify the point on the ARKit provided planes. Thus, we'll look for the intersection of the line of sight with the ARKit provided planes.</li>
<li>Notify the user of our search progress</li>
<li>Confirm the found result</li>
<li>Place and Orient the content in the desired direction</li>
</ol>


Step 1: Finding the intersection of the line of sight with the ARKit provided planes
************************************************************************************


## What are the concepts

It takes ARKit some time to find a plane in the cloud of 3D featurepoints. But it also takes some time to build this cloud: first, good featurepoints need to be found in the camera feed, and then they must be tracked from frame to frame. Eventually, ARKit analysis these found featurepoints to extract a horizontal or vertical plane. All this is done behind the scenes by the ARKit library.


So, we take a two step approach to finding a good location to place out content and to provide feedback to the user about the process:
1. First, from the moment there are featurepoints available, we find the featurepoint closest to our viewing vector, further called the "line-of-sight", and return this feauturepoint. This allows us to inform the user that featurepoints where found, but no plane could be extracted from them already.
2. Second, from the moment we find intersections of our line-of-sight vector with planes found by ARKit, we return this intersection. Now the user can point his device on a spot in that plane to place the AR content.


## How is it done in code?

The two step process is implemented in the folowing method of the PlaneFinding class:


	public static (SCNVector3? hitPoint, HitType hitType) FindNearestWorldPointToScreenPoint(CGPoint point, ARSCNView sceneView, SCNVector3? pointOnPlane)
	{
		var planeHitPosition = HitTestExistingPlanes(point, sceneView);
		if (planeHitPosition.HasValue)
			return (planeHitPosition, HitType.Plane);


		var pointCloudHitPosition = HitTestPointCloud(point, sceneView, 18, 0.2, 2.0);
		if (pointCloudHitPosition.HasValue)
			return (pointCloudHitPosition, HitType.FeaturePoint);


		return (null, HitType.None);
	}


Notice how we first check for intersections with planes by calling HitTestExistingPlanes and if that doesn't return any results, we continue searching the pointcloud.
		
Finding intersections with planes is done through the ARSCNView class. ARKit provides a method which does al the necesary calculations for us. We just need to provide the 2D coordinate in the view and the type of hittest result we want. We choose the ExistingPlaneUsingExtent type: it uses the smallest rectangular area including the ARKit detected plane. You can find other options and a more precise definition <a href="https://developer.apple.com/documentation/arkit/arhittestresult/resulttype">here</a>


The point in the view we use here is the center of the screen.
In the Update method of the ARGamePlay class we have:


	var screenRect = SceneView.Bounds;
	var screenCenter = new CGPoint(screenRect.GetMidX(), screenRect.GetMidY());


    var worldPos = PlaneFinding.FindNearestWorldPointToScreenPoint(screenCenter, SceneView, null);


In the 	FindNearestWorldPointToScreenPoint method we first look for intersections with planes found by ARKit through the <a href="https://developer.apple.com/documentation/arkit/arscnview/2875544-hittest">HitTest</a> method, as discussed above:


	private static SCNVector3? HitTestExistingPlanes(CGPoint point, ARSCNView sceneView)
	{
		var hitResult = sceneView.HitTest(point, ARHitTestResultType.ExistingPlaneUsingExtent);
		if (hitResult.Count() > 0)
		{
			var xPos = hitResult[0].WorldTransform.Column3.X;
			var yPos = hitResult[0].WorldTransform.Column3.Y;
			var zPos = hitResult[0].WorldTransform.Column3.Z;


			return new SCNVector3(xPos, yPos, zPos);
		}


		return null;        
	}


If none are found, we search for intersections with the feature point-cloud:


As you can see from the method signature, we have an argument specifying the angle of the cone into which we will search the closest point from the feature cloud. This cone starts in the middle of the screen and its axis is along the line of sight. Further arguments are a minimum and maximum from the top of the cone.


==(afbeelding nog te maken)==


We first start by finding the ray perpendicular to the iphones view (and thus in the direction of our line of sight) and whose origin is at the middle if the screen.
Next we iterate through all the featurepoints in the raw feature cloud we can obain from ARKit.
For each featurepoint: 
- We calculate its closest point on the line-of-sight ray. To calculate this distance we use the formula from wikipedia: <a href="https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line#Another_vector_formulation">Distance from a point to a line - Another vector formulation</a>
- Get its distance to the origin of the ray and if this distance is within the boundaries, continue. If not, we discard this feature point.
- Next, we calculate if the feature is within our cone: if not, we discard this feature point.
- Lastly, we check it this is the closest featurepoint found untill now: if not we discard it, else remember this feaurepoint.
	
In code, this loos like the following:


        private static SCNVector3? HitTestPointCloud(CGPoint point, ARSCNView sceneView, double coneOpeningAngleInDegrees, double minDistance = 0, double maxDistance = Double.MaxValue)
        {
            var minHitTestResultDistance = float.MaxValue;
            SCNVector3? closestFeauturePosition = null;


            if (sceneView.Session == null || ARGamePlay.CurrentFrame == null)
            {
                return null;
            }
            var features = ARGamePlay.CurrentFrame.RawFeaturePoints;
            if (features == null)
            {
                return null;
            }


            var ray = sceneView.HitTestRayFromScreenPos(point);
            if (ray == null)
            {
                return null;
            }


            var maxAngleInDeg = Math.Min(coneOpeningAngleInDegrees, 360) / 2.0;
            var maxAngle = (maxAngleInDeg / 180) * Math.PI;


            foreach (var featurePos in features.Points)
            {
                var scnFeaturePos = new SCNVector3(featurePos.X, featurePos.Y, featurePos.Z);
                var originToFeature = scnFeaturePos - ray.Origin;


                var hitTestResult = ray.Origin + (ray.Direction * (ray.Direction.Dot(originToFeature)));
				
				// Is the feature within the supplied distances?
                var hitTestResultDistance = (hitTestResult - ray.Origin).LengthFast;
                if (hitTestResultDistance < minDistance || hitTestResultDistance > maxDistance)
                {
                    // Skip this feature -- it's too close or too far
                    continue;
                }


				// Is the feature inside the cone?
                var originToFeatureNormalized = originToFeature.Normalized();
                var angleBetweenRayAndFeature = Math.Acos(ray.Direction.Dot(originToFeatureNormalized));
                if (angleBetweenRayAndFeature > maxAngle)
                {
                    // Skip this feature -- it's outside the cone 
                    continue;
                }


				// Is this the closest feature
                if(hitTestResultDistance < minHitTestResultDistance)
                {
                    minHitTestResultDistance = hitTestResultDistance;
                    closestFeauturePosition = hitTestResult;
                }
            }


            return closestFeauturePosition;
        }


So, how do we find the line-of-sight ray?


As you may, or may not, know: the renderer has a near clipping plane and a far clipping plane. Everything before the near clipping plane and everything behind the far clipping plane is not rendered.


To calculate the line-of-sight we "unproject" the center of our 2D view in the 3D world. As can be seen on following picture, this "unprojection" can be anywhere on a line. That is why we tell the function to use the far clipping plane. Our line-of-sight then is the vector going from this unprojected point, to the osition of the camera.


Because a picture is worth a thousand words:


==(afbeelding nog te maken)==


In code we get:


	public static Ray HitTestRayFromScreenPos(this ARSCNView sceneView, CGPoint point)
	{
		var frame = ARGamePlay.CurrentFrame;
		if (frame == null || frame.Camera == null || frame.Camera.Transform == null)
		{
			return null;
		}


		var cameraPos = SCNVector3Ex.PositionFromTransform(frame.Camera.Transform);


		// setting z = 1.0 will unproject the screen position to the far clipping plane.
		// see also https://developer.apple.com/documentation/scenekit/scnscenerenderer/1522631-unprojectpoint
		var positionVec = new SCNVector3((float)point.X, (float)point.Y, 1.0f);
		var screenPosOnFarClippingPlane = sceneView.UnprojectPoint(positionVec);


		var rayDirection = screenPosOnFarClippingPlane - cameraPos;
		// We simply want the direction, so normalize to vector with length 1
		rayDirection.Normalize();


		return new Ray(cameraPos, rayDirection);
	} 


Step 2: Notifying the user of the planefinding progress:
********************************************************
Of course, during the search for the intersection of the line of sight with the ARKit found planes, we would like to keep our user informed of the progress we make.
This progress has 4 distinct states:
<ol>
<li>ARKit hasn't found anything usefull yet: no featurepoints and no planes</li>
<li>ARKit has found featurepoints, but no planes</li>
<li>ARKit has found planes, but they do not intersect with our line of sight.</li>  ==KUNNEN WE HIER OP DIFFERENTIEREN? WETEN WIJ DAT? TE BEKIJKEN IN DE BOVENSTAANDE CODE WAAR DE INTERSECTIE GEZOCHT WORDT !!!==
<li>ARKit has found planes which intersect with our line of sight</li>
</ol>


wat uitleg over IARSCNViewDelegate, zie in ViewController+ARSCNViewDelegate.cs


	
	
Step x: placing and orienting content:
**************************************





References
**********
https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/device-provisioning/free-provisioning?tabs=macos