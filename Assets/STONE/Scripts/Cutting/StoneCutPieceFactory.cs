using System;
using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEngine;

namespace StoneGame.Cutting
{
    public static class StoneCutPieceFactory
    {
        public static CutStoneHalf Create(Mesh mesh,Material[] materials,Vector3 center,Vector3 normal,float radius,float volume,
            Vector3 position,Quaternion rotation,TidyObject source,StoneCutProfile profile,string suffix,OreDefinition ore,
            StoneMeshCutSettings settings,float massFraction,int cutSurfaceSubmesh=-1)
        {
            GameObject root=null; TidyPhysicsProfile physics=null;
            try
            {
                root=new GameObject(source.name+"_"+suffix); root.SetActive(false); root.layer=source.gameObject.layer;
                root.transform.SetPositionAndRotation(position,rotation);
                root.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=root.AddComponent<MeshRenderer>(); renderer.sharedMaterials=materials;
                var sourceRenderer=source.GetComponentInChildren<MeshRenderer>();
                if(sourceRenderer!=null)
                {
                    renderer.shadowCastingMode=sourceRenderer.shadowCastingMode; renderer.receiveShadows=sourceRenderer.receiveShadows;
                    renderer.lightProbeUsage=sourceRenderer.lightProbeUsage; renderer.reflectionProbeUsage=sourceRenderer.reflectionProbeUsage;
                }
                var body=root.AddComponent<Rigidbody>();
                if(source.Body!=null)
                {
                    body.constraints=source.Body.constraints; body.maxAngularVelocity=source.Body.maxAngularVelocity;
                    body.sleepThreshold=source.Body.sleepThreshold;
                }
                body.isKinematic=true;
                CreateColliders(root,mesh,source,settings);
                var tidy=root.AddComponent<TidyObject>();
                tidy.AssignDefinition(profile.CutPieceTidyDefinition!=null?profile.CutPieceTidyDefinition:source.Definition);
                tidy.AssignInstanceId(source.InstanceId+"_"+suffix);
                physics=UnityEngine.Object.Instantiate(source.PhysicsProfile);
                physics.hideFlags=HideFlags.HideAndDontSave;
                physics.Configure(source.PhysicsProfile.InteractionMode,Mathf.Max(.01f,source.PhysicsProfile.Mass*massFraction));
                tidy.AssignPhysicsProfileOverride(physics);
                var half=root.AddComponent<CutStoneHalf>();
                half.InitializeGenerated(mesh,physics,center,normal,radius,volume,ore,source.InstanceId,profile,settings,cutSurfaceSubmesh);
                physics=null; // Ownership belongs to CutStoneHalf from here.
                root.SetActive(true);
                tidy.RefreshCachedComponents(); tidy.MarkLoose();
                return half;
            }
            catch
            {
                if(root!=null)
                {
                    var tidy=root.GetComponent<TidyObject>(); if(tidy!=null) TidyObjectRegistry.Unregister(tidy);
                    root.SetActive(false); StoneMeshCutResult.DestroyMesh(root);
                }
                StoneMeshCutResult.DestroyMesh(physics);
                throw;
            }
        }

        static void CreateColliders(GameObject root,Mesh mesh,TidyObject source,StoneMeshCutSettings settings)
        {
            var triangles=mesh.triangles;
            PhysicsMaterial material=null;
            foreach(var collider in source.Colliders) if(collider!=null) { material=collider.sharedMaterial; break; }
            // The vertex bound also protects disconnected simple shells: their combined hull can
            // exceed PhysX's face limit even when their original triangle count is modest.
            bool smallHull = triangles.Length/3<=settings.convexTriangleLimit;
            if(smallHull) smallHull = new HashSet<Vector3>(mesh.vertices).Count<=100;
            if(smallHull)
            {
                var collider=root.AddComponent<MeshCollider>();
                collider.convex=true; collider.sharedMaterial=material; collider.sharedMesh=mesh;
                return;
            }
            // PhysX convex meshes have a face limit and cannot follow concavities. A bounded compound
            // grid follows complex silhouettes without invalid dynamic, non-convex MeshColliders.
            int resolution=Mathf.Clamp(settings.compoundColliderResolution,2,6);
            var bounds=mesh.bounds; var vertices=mesh.vertices;
            float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))/resolution;
            if(size<=1e-7f) throw new ArgumentException("Cut result is too small for a reliable collider.");
            int nx=Mathf.Max(1,Mathf.CeilToInt(bounds.size.x/size));
            int ny=Mathf.Max(1,Mathf.CeilToInt(bounds.size.y/size));
            int nz=Mathf.Max(1,Mathf.CeilToInt(bounds.size.z/size));
            var step=new Vector3(bounds.size.x/nx,bounds.size.y/ny,bounds.size.z/nz);
            var occupied=new bool[nx*ny*nz];
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]]; var b=vertices[triangles[i+1]]; var c=vertices[triangles[i+2]];
                var min=Vector3.Min(a,Vector3.Min(b,c))-bounds.min;
                var max=Vector3.Max(a,Vector3.Max(b,c))-bounds.min;
                int x0=Mathf.Clamp(Mathf.FloorToInt(min.x/step.x),0,nx-1),x1=Mathf.Clamp(Mathf.FloorToInt(max.x/step.x),0,nx-1);
                int y0=Mathf.Clamp(Mathf.FloorToInt(min.y/step.y),0,ny-1),y1=Mathf.Clamp(Mathf.FloorToInt(max.y/step.y),0,ny-1);
                int z0=Mathf.Clamp(Mathf.FloorToInt(min.z/step.z),0,nz-1),z1=Mathf.Clamp(Mathf.FloorToInt(max.z/step.z),0,nz-1);
                for(int x=x0;x<=x1;x++) for(int y=y0;y<=y1;y++) for(int z=z0;z<=z1;z++) occupied[(x*ny+y)*nz+z]=true;
            }
            int count=0;
            for(int x=0;x<nx;x++) for(int y=0;y<ny;y++) for(int z=0;z<nz;z++)
            {
                if(!occupied[(x*ny+y)*nz+z]) continue;
                int start=z;
                while(z+1<nz && occupied[(x*ny+y)*nz+z+1]) z++;
                int length=z-start+1;
                var collider=root.AddComponent<BoxCollider>();
                collider.center=bounds.min+Vector3.Scale(step,new Vector3(x+.5f,y+.5f,start+length*.5f));
                collider.size=new Vector3(step.x,step.y,step.z*length); collider.sharedMaterial=material; count++;
            }
            if(count==0) throw new ArgumentException("Could not build the cut piece's compound collider.");
        }

        public static void SeparateOutputs(CutStoneHalf left,CutStoneHalf right,Vector3 direction,float gap)
        {
            direction=direction.sqrMagnitude>1e-8f?direction.normalized:Vector3.right;
            var a=left.GetComponent<TidyObject>().CalculateWorldBounds();
            var b=right.GetComponent<TidyObject>().CalculateWorldBounds();
            float radiusA=Vector3.Dot(a.extents,new Vector3(Mathf.Abs(direction.x),Mathf.Abs(direction.y),Mathf.Abs(direction.z)));
            float radiusB=Vector3.Dot(b.extents,new Vector3(Mathf.Abs(direction.x),Mathf.Abs(direction.y),Mathf.Abs(direction.z)));
            float overlap=radiusA+radiusB+gap-Vector3.Dot(b.center-a.center,direction);
            if(overlap>0)
            {
                left.transform.position-=direction*(overlap*.5f);
                right.transform.position+=direction*(overlap*.5f);
            }
        }
    }
}
