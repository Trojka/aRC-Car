﻿using System;
using System.Diagnostics;
using System.Drawing;
using CoreGraphics;
using SpriteKit;
using UIKit;

namespace aRCCar.Xamarin.Game
{
    public class OverlayScene : SKScene
    {
        SKLabelNode _scanText1;
        SKLabelNode _scanText2;
        SKSpriteNode _phone;

        SKSpriteNode _check;

        SKSpriteNode _pistonLeft;
        SKSpriteNode _pistonRight;

        public OverlayScene(CGSize sceneSize) : base(sceneSize)
        {
            Debug.WriteLine("OverlayScene: " + base.Size.ToString());

            BackgroundColor = UIColor.Clear;
            var size = base.Size;


        }

        public void ShowScanAction()
        {
            var size = base.Size;
            _scanText1 = new SKLabelNode("AppleSDGothicNeo-Regular")
            {
                Text = "Move your phone",
                FontSize = 30,
                Position = new CGPoint(size.Width / 2, 100),
                Color = UIColor.White,
            };
            AddChild(_scanText1);

            _scanText2 = new SKLabelNode("AppleSDGothicNeo-Regular")
            {
                Text = "to find a surface",
                FontSize = 30,
                Position = new CGPoint(size.Width / 2, 70),
                Color = UIColor.White,
            };
            AddChild(_scanText2);

            _phone = SKSpriteNode.FromImageNamed("phone_scaled");
            _phone.Position = new CGPoint(size.Width / 2, 190);

            var circle = UIBezierPath.FromRoundedRect(new CGRect(new CGPoint(size.Width / 2 - 20, 190), new CGSize(40, 40)), 20);
            var moveAlongPath = SKAction.RepeatActionForever(SKAction.FollowPath(circle.CGPath, false, false, 2.0));
            _phone.RunAction(moveAlongPath);

            AddChild(_phone);

            ScanActionShowing = true;
            ScanActionFinished = false;
        }

        public bool ScanActionShowing 
        {
            get;
            private set;
        }

        public void FinishScanAction()
        {
            if(_scanText1 != null)
            {
                _scanText1.RemoveFromParent();
                _scanText1 = null;
            }
            if(_scanText2 != null)
            {
                _scanText2.RemoveFromParent();
                _scanText2 = null;
            }
            if(_phone != null)
            {
                _phone.RemoveAllActions();
                _phone.RemoveFromParent();
                _phone = null;
            }

            ScanActionShowing = false;
            ScanActionFinished = true;
        }

        public bool ScanActionFinished
        {
            get;
            private set;
        }

        public void ShowActionPlacement()
        {
            var size = base.Size;
            _check = SKSpriteNode.FromImageNamed("check_scaled");
            _check.Position = new CGPoint(size.Width / 2, 190);
            AddChild(_check);
        }

        public void FinishActionPlacement()
        {
            if(_check != null)
            {
                _check.RemoveFromParent();
                _check = null;
            }
        }

        public void ShowActionControls()
        {
            var size = base.Size;

            _pistonLeft = SKSpriteNode.FromImageNamed("piston_scaled");
            _pistonLeft.Position = new CGPoint(size.Width / 2 - 45, 150);

            AddChild(_pistonLeft);

            _pistonRight = SKSpriteNode.FromImageNamed("piston_scaled");
            _pistonRight.Position = new CGPoint(size.Width / 2 + 45, 150);

            AddChild(_pistonRight);
        }
    }
}