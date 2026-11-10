/**
  @file Network_Demo/Browse.cs

  Port of Network_Demo/Browse.h and Browse.cpp.

  The applet that first appears and lets the user
  choose to join an existing game or host a new
  game.
 */

using System;
using G3D;

namespace Network_Demo
{
    public class Browse : GApplet
    {
        private App app;

        private DiscoveryClient<ServerAd> discoveryClient = new DiscoveryClient<ServerAd>();

        /**
          The last button is exit, the 2nd to last
          is host, the others correspond to discoveryClient
          servers.
         */
        private Array<Rect2D> button = new Array<Rect2D>();

        /** When a server is selected, this field is set */
        public ServerAd selectedServer = new ServerAd();

        /** Whether this machine should host */
        public bool host;

        public Browse(App app)
            : base(app)
        {
            this.app = app;
        }

        public override void init()
        {
            try
            {
                discoveryClient.init(app.networkDevice, app.discoverySettings);
            }
            catch (Exception)
            {
                // The discovery ports are already in use, most likely because
                // another copy of this demo is running.  The Browse menu cannot
                // work without discovery, so report it and quit rather than
                // failing later with an unrelated error.
                G3D.Error.error("Network Demo Error",
                    "Unable to listen on the network discovery port. Another copy of the Network Demo may already be running.",
                    true);
                app.endProgram = true;
                endApplet = true;
                return;
            }

            host = false;
        }

        public override void doLogic()
        {
            if (app.userInput.keyPressed((UserInput.KeyCode)UserInput.CustomKeyCode.SDL_LEFT_MOUSE_KEY))
            {
                int HOST = button.size() - 2;
                int EXIT = button.size() - 1;
                Vector2 mouse = app.userInput.mouseXY();
                // See what button the user pressed
                for (int b = 0; b < button.size(); ++b)
                {
                    if (button[b].contains(mouse))
                    {
                        if (b == HOST)
                        {
                            host = true;
                            endApplet = true;
                            break;
                        }
                        else if (b == EXIT)
                        {
                            app.endProgram = true;
                            endApplet = true;
                            break;
                        }
                        else if (discoveryClient.serverList.size() > b)
                        {
                            selectedServer = discoveryClient.serverList[b];
                            endApplet = true;
                            break;
                        }
                    }
                }
            }

            if (app.userInput.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                endApplet = true;
                app.endProgram = true;
            }
        }

        public override void doNetwork()
        {
            // Allow the discovery client talk to the
            // network and search for servers.
            discoveryClient.doNetwork();
        }

        public override void doSimulation(RealTime rdt)
        {
        }

        public override void doGraphics()
        {
            app.renderDevice.setColorClearValue(new Color4(Color3.white(), 1));
            app.renderDevice.clear(true, true, true);

            app.renderDevice.push2D();
            double width = app.renderDevice.width();
            double height = app.renderDevice.height();
            double titleFontSize = 30 * width / 800;
            double optionFontSize = 25 * width / 800;
            Color4 titleFontColor = new Color4(Color3.black(), 1);
            Color4 titleFontBorder = new Color4(Color3.white(), 0);
            Color4 optionFontColor = new Color4(Color3.yellow(), 1);
            Color4 optionFontBorder = new Color4(Color3.black(), 1);

            double y = 10;
            app.font.pointer().draw2D(app.renderDevice, "Choose A Network Demo Server", new Vector2((float)(width / 2), (float)y),
                titleFontSize, titleFontColor, titleFontBorder, GFont.XAlign.XALIGN_CENTER);
            y += titleFontSize * 2;

            button.clear();

            // Display the servers found
            for (int s = 0; s < discoveryClient.serverList.size(); ++s)
            {
                Vector2 bounds = app.font.pointer().draw2D(app.renderDevice, discoveryClient.serverList[s].name,
                    new Vector2((float)(width / 2), (float)y),
                    optionFontSize, optionFontColor,
                    optionFontBorder, GFont.XAlign.XALIGN_CENTER);

                button.append(Rect2D.xywh((float)(width / 2 - bounds.x / 2), (float)y, bounds.x, bounds.y));

                y += optionFontSize * 2;
            }

            if (discoveryClient.serverList.size() == 0)
            {
                app.font.pointer().draw2D(app.renderDevice, "(No servers found-- host one yourself!)",
                    new Vector2((float)(width / 2), (float)y),
                    optionFontSize, titleFontColor,
                    titleFontBorder, GFont.XAlign.XALIGN_CENTER);

                y += optionFontSize * 2;
            }

            y = height - optionFontSize * 6;

            Vector2 bounds2 = app.font.pointer().draw2D(app.renderDevice, "Create Server", new Vector2((float)(width / 2), (float)y),
                optionFontSize, optionFontColor, optionFontBorder, GFont.XAlign.XALIGN_CENTER);
            button.append(Rect2D.xywh((float)(width / 2 - bounds2.x / 2), (float)y, bounds2.x, bounds2.y));
            y += optionFontSize * 2;

            bounds2 = app.font.pointer().draw2D(app.renderDevice, "Exit", new Vector2((float)(width / 2), (float)y),
                optionFontSize, optionFontColor, optionFontBorder, GFont.XAlign.XALIGN_CENTER);
            button.append(Rect2D.xywh((float)(width / 2 - bounds2.x / 2), (float)y, bounds2.x, bounds2.y));
            y += optionFontSize * 2;

            app.renderDevice.pop2D();
        }

        public override void cleanup()
        {
            discoveryClient.cleanup();
        }
    }
}
