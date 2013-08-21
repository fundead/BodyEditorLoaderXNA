using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Common;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace callum.bodyeditorloader
{

    /**
     * Loads the collision fixtures defined with the Physics Body Editor
     * application. You only need to give it a body and the corresponding fixture
     * name, and it will attach these fixtures to your body.
     *
     * @author Aurelien Ribon | http://www.aurelienribon.com | Ported to .NET by Fundead | http://pixelpegasus.cloudns.net
     */

    public class BodyEditorLoader
    {

        // Model
        private Model model;

        // Reusable stuff
        private List<Vector2> vectorPool = new List<Vector2>();
        private PolygonShape polygonShape = new PolygonShape(0);
        private CircleShape circleShape = new CircleShape(0, 0);
        private Vector2 vec = new Vector2();

        // -------------------------------------------------------------------------
        // Ctors
        // -------------------------------------------------------------------------

        public BodyEditorLoader(string filePath)
        {
            string file = File.ReadAllText(filePath);
            if (file == null)
                throw new NullReferenceException("file is null");
            model = readJson(file);
        }

        //public BodyEditorLoader(string str) {
        //    if (str == null) throw new NullReferenceException("str is null");
        //    model = readJson(str);
        //}

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /**
         * Creates and applies the fixtures defined in the editor. The name
         * parameter is used to retrieve the right fixture from the loaded file.
         * <br/><br/>
         *
         * The body reference point (the red cross in the tool) is by default
         * located at the bottom left corner of the image. This reference point
         * will be put right over the BodyDef position point. Therefore, you should
         * place this reference point carefully to let you place your body in your
         * world easily with its BodyDef.position point. Note that to draw an image
         * at the position of your body, you will need to know this reference point
         * (see {@link #getOrigin(java.lang.String, float)}.
         * <br/><br/>
         *
         * Also, saved shapes are normalized. As shown in the tool, the width of
         * the image is considered to be always 1 meter. Thus, you need to provide
         * a scale factor so the polygons get resized according to your needs (not
         * every body is 1 meter large in your game, I guess).
         *
         * @param body The Box2d body you want to attach the fixture to.
         * @param name The name of the fixture you want to load.
         * @param fd The fixture parameters to apply to the created body fixture.
         * @param scale The desired scale of the body. The default width is 1.
         */

        public void attachFixture(Body body, String name, float scale)
        {
            // deleted FixtureDef
            RigidBodyModel rbModel = model.rigidBodies[name];
            if (rbModel == null) throw new SystemException("Name '" + name + "' was not found.");

            vec = rbModel.origin*scale;
            Vector2 origin = vec;

            for (int i = 0, n = rbModel.polygons.Count; i < n; i++)
            {
                PolygonModel polygon = rbModel.polygons[i];
                Vertices vertices = new Vertices(polygon.vertices);

                for (int ii = 0, nn = vertices.Count; ii < nn; ii++)
                {
                    var v = NewVec();
                    v = vertices[ii]*scale;
                    vertices[ii] = v;
                    vertices[ii] -= origin;
                }

                polygonShape.Set(vertices);
                body.CreateFixture(polygonShape);

                for (int ii = 0, nn = vertices.Count; ii < nn; ii++)
                {
                    Free(vertices[ii]);
                }
            }

            for (int i = 0, n = rbModel.circles.Count; i < n; i++)
            {
                CircleModel circle = rbModel.circles[i];
                var v2 = NewVec();
                v2 = circle.center*scale;
                Vector2 center = v2;
                float radius = circle.radius*scale;

                circleShape.Position = center;
                circleShape.Radius = radius;
                body.CreateFixture(circleShape);

                Free(center);
            }
        }

        /**
         * Gets the image path attached to the given name.
         */

        public String getImagePath(String name)
        {
            RigidBodyModel rbModel = model.rigidBodies[name];
            if (rbModel == null) throw new SystemException("Name '" + name + "' was not found.");

            return rbModel.imagePath;
        }

        /**
         * Gets the origin point attached to the given name. Since the point is
         * normalized in [0,1] coordinates, it needs to be scaled to your body
         * size. Warning: this method returns the same Vector2 object each time, so
         * copy it if you need it for later use.
         */

        public Vector2 getOrigin(String name, float scale)
        {
            RigidBodyModel rbModel = model.rigidBodies[name];
            if (rbModel == null) throw new SystemException("Name '" + name + "' was not found.");

            return vec = rbModel.origin*scale;
        }

        /**
         * <b>For advanced users only.</b> Lets you access the internal model of
         * this loader and modify it. Be aware that any modification is permanent
         * and that you should really know what you are doing.
         */

        public Model getInternalModel()
        {
            return model;
        }

        // -------------------------------------------------------------------------
        // Json Models
        // -------------------------------------------------------------------------

        public class Model
        {
            public Dictionary<String, RigidBodyModel> rigidBodies = new Dictionary<string, RigidBodyModel>();
        }

        public class RigidBodyModel
        {
            public String name;
            public String imagePath;
            public Vector2 origin = new Vector2();
            public List<PolygonModel> polygons = new List<PolygonModel>();
            public List<CircleModel> circles = new List<CircleModel>();
        }

        public class PolygonModel
        {
            public List<Vector2> vertices = new List<Vector2>();
            public Vertices buffer; // used to avoid allocation in attachFixture()
        }

        public class CircleModel
        {
            public Vector2 center = new Vector2();
            public float radius;
        }

        // -------------------------------------------------------------------------
        // Json reading process
        // -------------------------------------------------------------------------

        private Model readJson(String str)
        {
            Model m = new Model();

            OrderedDictionary rootElem = JsonConvert.DeserializeObject<OrderedDictionary>(str);

            var array = rootElem["rigidBodies"] as JArray;            

            for (int i = 0; i < array.Count; i++)
            {
                OrderedDictionary bodyElem = array[i].ToObject<OrderedDictionary>();
                RigidBodyModel rbModel = readRigidBody(bodyElem);                
                m.rigidBodies.Add(rbModel.name, rbModel);
            }

            return m;
        }

        private RigidBodyModel readRigidBody(OrderedDictionary bodyElem)
        {
            RigidBodyModel rbModel = new RigidBodyModel();
            rbModel.name = (String) bodyElem["name"];
            rbModel.imagePath = (String) bodyElem["imagePath"];

            var bodyElemObject = bodyElem["origin"] as JObject;
            OrderedDictionary originElem = bodyElemObject.ToObject<OrderedDictionary>();

            rbModel.origin.X = Convert.ToSingle(originElem["x"]);
            rbModel.origin.Y = Convert.ToSingle(originElem["y"]);
            

            // polygons
            var bEA = bodyElem["polygons"] as JArray;            

            for (int i = 0; i < bEA.Count; i++)
            {
                PolygonModel polygon = new PolygonModel();
                rbModel.polygons.Add(polygon);

                var verticesElem = bEA[i] as JArray;                
                for (int ii = 0; ii < verticesElem.Count; ii++)
                {
                    OrderedDictionary vertexElem = verticesElem[ii].ToObject<OrderedDictionary>();
                    float x = Convert.ToSingle(vertexElem["x"]);
                    float y = Convert.ToSingle(vertexElem["y"]);

                    polygon.vertices.Add(new Vector2(x, y));
                }

                polygon.buffer = new Vertices(polygon.vertices.Count);
            }

            // circles
            
            var circlesElem = bodyElem["circles"] as JArray;

            for (int i = 0; i < circlesElem.Count; i++)
            {
                CircleModel circle = new CircleModel();
                rbModel.circles.Add(circle);

                OrderedDictionary circleElem = circlesElem[i].ToObject<OrderedDictionary>();
                circle.center.X = (float) circleElem["cx"];
                circle.center.Y = (float) circleElem["cy"];
                circle.radius = (float) circleElem["r"];
            }

            return rbModel;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private Vector2 NewVec()
        {
            if (vectorPool.Count == 0)
            {
                return new Vector2();
            }

            var v = vectorPool[0];
            vectorPool.RemoveAt(0);
            return v;
        }

        private void Free(Vector2 v)
        {
            vectorPool.Add(v);
        }
    }
}