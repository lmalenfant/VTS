﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Vts.Modeling.ForwardSolvers.MGRTE._2D.DataStructures;

namespace Vts.Modeling.ForwardSolvers.MGRTE._2D.IO
{
    class Initialization
    {
        public static void Initial(
            int alevel, int alevel0, 
            int slevel, int slevel0, 
            double[][][] mua, 
            double[][][] mus, 
            bool vacuum, 
            double index_i, 
            double index_o,
            int level, 
            int whichmg, 
            int[][] noflevel,
            ref AngularMesh[] amesh,
            ref SpatialMesh[] smesh, 
            ref double[][][][] flux,
            ref double[][][][] d, 
            ref double[][][][] RHS,
            ref double[][][][] q, 
            ref BoundaryCoupling[] b)
        {
            

             
            
             //         we first load the mesh files:           
             //                       
             //         and then we compute the following for spatial mesh:
             //             2.1. "smap", "cf" and "fc"            
             //         and finally malloc the following for multigrid algorithm:
             //             3.1. "noflevel"
             //             3.2. "mua", "mus", "flux", "RHS", "d" and "q"
             //             3.3.  "b"

            int tempsize = 20, tempsize2 = 20;
            int i, j, k,m, n, nt, np, ne, ns, da = alevel - alevel0, ds = slevel - slevel0;
            double x1,x2,x3,y1,y2,y3;
            int[][] t;
            int[][] e;
            int[][] p2;
            int[][] smap;
            double[][] p;

                     

            // 2.1. compute "c", "ec", "a" and "p2"
            //      p2[np][p2[np][0]+1]: triangles adjacent to one node
            //      For the 2nd index, the 1st element is the total number of triangles adjacent to this node,
            //      the corresponding triangles are saved from the 2nd.
            //      Example:    assume triangles [5 16 28 67] are adjacent to the node 10, then
            //                  p2[10][0]=4, p2[10][1]=5, p2[10][2]=16, p2[10][3]=28 and p2[10][4]=67.
            for (i = 0; i <= slevel; i++)
            {
                p = smesh[i].p; t = smesh[i].t; nt = smesh[i].nt; np = smesh[i].np; e = smesh[i].e; ne = smesh[i].ne;

                smesh[i].c = new double[nt][];
                for (j = 0; j < nt; j++)
                {
                    smesh[i].c[j] = new double[2];                    
                    for (k = 0; k < 2; k++)
                    { 
                        smesh[i].c[j][k] = (p[t[j][0]][k] + p[t[j][1]][k] + p[t[j][2]][k]) / 3; 
                    }// center of triangle
                }

                smesh[i].ec = new double[ne][];
                for (j = 0; j < ne; j++)
                {
                    smesh[i].ec[j] = new double[2]; ;
                    for (k = 0; k < 2; k++)
                    { smesh[i].ec[j][k] = (p[e[j][1]][k] + p[e[j][2]][k]) / 2; }// center of edge
                }

                smesh[i].a = new double[nt]; ;
                for (j = 0; j < nt; j++)
                {
                    x1 = p[t[j][0]][0]; y1 = p[t[j][0]][1];
                    x2 = p[t[j][1]][0]; y2 = p[t[j][1]][1];
                    x3 = p[t[j][2]][0]; y3 = p[t[j][2]][1];
                    smesh[i].a[j] =  MultiGridCycle.Area(x1, y1, x2, y2, x3, y3);//area of triangle
                }

                p2 = new int[np][];               
                for (j = 0; j < np; j++)
                {
                    p2[j] = new int[tempsize];
                    // tempsize is the initial length of the second index of "p2", and it may cause problem if it is too small.
                    p2[j][0] = 0;
                    for (k = 1; k < tempsize; k++)
                    { 
                        p2[j][k] = -1; 
                    }
                }

                for (j = 0; j < nt; j++)
                {
                    for (k = 0; k < 3; k++)
                    {
                        p2[t[j][k]][0] += 1;
                        p2[t[j][k]][p2[t[j][k]][0]] = j;
                    }
                }

                for (j = 0; j < np; j++)
                {
                    if (p2[j][0] + 1 > tempsize)
                    {
                        Console.WriteLine("WARNING: tempsize for p2 is too small!!\n");
                        goto stop;
                    }
                }

                smesh[i].p2 = new int[np][];
                for (j = 0; j < np; j++)
                {
                    smesh[i].p2[j] = new int[(p2[j][0] + 1)];
                    for (k = 0; k <= p2[j][0]; k++)
                    { smesh[i].p2[j][k] = p2[j][k]; }
                }               
            }

            // 2.2. compute "smap", "cf" and "fc"
            //      For the data structure of "smap", see "spatialmapping";
            //      For the data structure of "cf" and "fc", see "spatialmapping2".
            //      Note: those arrays are not defined on the coarsest spatial mesh (i=0),
            //      since they are always saved on the fine level instead of the coarse level.
            for (i = 1; i <= slevel; i++)
            {
                smap = new int [smesh[i - 1].nt][];
                for (j = 0; j < smesh[i - 1].nt; j++)
                {
                    smap[j] = new int[tempsize2];
                    // tempsize2 is the initial length of the second index of "smap", and it may cause problem if it is too small.
                    smap[j][0] = 0;
                    for (k = 1; k < tempsize2; k++)
                    { smap[j][k] = -1; }
                }
                 MultiGridCycle.SpatialMapping(smesh[i - 1], smesh[i], smap);

                for (j = 0; j < smesh[i - 1].nt; j++)
                {
                    if (smap[j][0] > tempsize2 - 1)
                    {
                        Console.WriteLine("WARNING: tempsize2 for smap is too small!!\n");
                        goto stop;
                    }
                }

                smesh[i].smap = new int [smesh[i - 1].nt][];
                for (j = 0; j < smesh[i - 1].nt; j++)
                {
                    smesh[i].smap[j] = new int [smap[j][0] + 1];
                    for (k = 0; k <= smap[j][0]; k++)
                    { smesh[i].smap[j][k] = smap[j][k]; }
                }


                smesh[i].cf = new double[smesh[i - 1].nt][][][];
                for (j = 0; j < smesh[i - 1].nt; j++)
                {
                    smesh[i].cf[j] = new double[3][][];
                    for (k = 0; k < 3; k++)
                    {
                        smesh[i].cf[j][k] = new double[smesh[i].smap[j][0]][]; 
                        for (m = 0; m < smesh[i].smap[j][0]; m++)
                        { smesh[i].cf[j][k][m] = new double[3]; ; }
                    }
                }
                smesh[i].fc = new double[smesh[i - 1].nt][][][];
                for (j = 0; j < smesh[i - 1].nt; j++)
                {
                    smesh[i].fc[j] = new double[3][][];
                    for (k = 0; k < 3; k++)
                    {
                        smesh[i].fc[j][k] = new double[smesh[i].smap[j][0]][];
                        for (m = 0; m < smesh[i].smap[j][0]; m++)
                        { smesh[i].fc[j][k][m] = new double[3]; ; }
                    }
                }
                 MultiGridCycle.SpatialMapping2(smesh[i - 1], smesh[i], smesh[i].smap, smesh[i].cf, smesh[i].fc, tempsize2);
            }

            // 2.3. compute "e", "e2", "so2", "n" and "ori"
            //      For the data structure of "eo", "e2","so2", "n" and "ori", see "boundary".
            for (i = 0; i <= slevel; i++)
            {
                smesh[i].e2 = new int [smesh[i].ne][];
                for (j = 0; j < smesh[i].ne; j++)
                { smesh[i].e2[j] = new int[2]; }
                smesh[i].so2 = new int [smesh[i].nt][];
                for (j = 0; j < smesh[i].nt; j++)
                { smesh[i].so2[j] = new int[3]; }
                smesh[i].n = new double[smesh[i].ne][];
                for (j = 0; j < smesh[i].ne; j++)
                { smesh[i].n[j] = new double[2]; }
                smesh[i].ori = new int[smesh[i].ne];

                 MultiGridCycle.Bound(smesh[i].ne, smesh[i].nt, smesh[i].t, smesh[i].p2, smesh[i].p, smesh[i].e, smesh[i].e2, smesh[i].so2, smesh[i].n, smesh[i].ori);
            }

            // 2.4. compute "bd" and "bd2"
            //      For the data structure of "bd" and "bd2", see "edgeterm".
            for (i = 0; i <= slevel; i++)
            {
                smesh[i].bd = new int[amesh[alevel].ns][][];
                for (j = 0; j < amesh[alevel].ns; j++)
                {
                    smesh[i].bd[j] = new int [smesh[i].nt][];;
                    for (k = 0; k < smesh[i].nt; k++)
                    {
                        smesh[i].bd[j][k] = new int [9];
                        for (m = 0; m < 9; m++)
                        { smesh[i].bd[j][k][m] = -1; }
                    }
                }
            }
            for (i = 0; i <= slevel; i++)
            {
                smesh[i].bd2 = new double [amesh[alevel].ns][][];
                for (j = 0; j < amesh[alevel].ns; j++)
                {
                    smesh[i].bd2[j] = new double[smesh[i].nt][];
                    for (k = 0; k < smesh[i].nt; k++)
                    { smesh[i].bd2[j][k] = new double[3];}
                }
            }
            for (i = 0; i <= slevel; i++)
            {
                for (j = 0; j < amesh[alevel].ns; j++)
                {  MultiGridCycle.EdgeTri(smesh[i].nt, amesh[alevel].a[j], smesh[i].p, smesh[i].p2, smesh[i].t, smesh[i].bd[j], smesh[i].bd2[j], smesh[i].so2); }
            }

            // 3.1. compute "noflevel"
            //      level: the mesh layers for multgrid; the bigger value represents finer mesh.
            //      noflevel[level][2]: the corresponding angular mesh level and spatial mesh level for each multigrid mesh level
            //      Example:    assume noflevel[i][0]=3 and noflevel[i][1]=2, then
            //                  the spatial mesh level is "3" and the angular mesh level is "2" on the multgrid mesh level "i".
            switch (whichmg)
            {
                case 1: //AMG
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    for (i = 0; i <= da; i++)
                    {
                        noflevel[i][0] = slevel;
                        noflevel[i][1] = i + alevel0;
                    }
                    break;
                case 2: //SMG
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    for (i = 0; i <= ds; i++)
                    {
                        noflevel[i][0] = i + slevel0;
                        noflevel[i][1] = alevel;
                    }
                    break;
                case 3: //MG1
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    if (ds > da)
                    {
                        for (i = 0; i < ds - da; i++)
                        {
                            noflevel[i][0] = i + slevel0;
                            noflevel[i][1] = alevel0;
                        }
                        for (i = 0; i <= da; i++)
                        {
                            noflevel[i + ds - da][0] = i + ds - da + slevel0;
                            noflevel[i + ds - da][1] = i + alevel0;
                        }
                    }
                    else
                    {
                        for (i = 0; i < da - ds; i++)
                        {
                            noflevel[i][0] = slevel0;
                            noflevel[i][1] = i + alevel0;
                        }
                        for (i = 0; i <= ds; i++)
                        {
                            noflevel[i + da - ds][0] = i + slevel0;
                            noflevel[i + da - ds][1] = i + da - ds + alevel0;
                        }
                    }
                    break;
                case 4: //MG2
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    for (i = slevel0; i <= slevel; i++)
                    {
                        noflevel[i - slevel0][0] = i;
                        noflevel[i - slevel0][1] = alevel0;
                    }
                    for (i = alevel0 + 1; i <= alevel; i++)
                    {
                        noflevel[i - alevel0 + slevel - slevel0][0] = slevel;
                        noflevel[i - alevel0 + slevel - slevel0][1] = i;
                    }
                    break;
                case 5: //MG3
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    for (i = alevel0; i <= alevel; i++)
                    {
                        noflevel[i - alevel0][0] = slevel0;
                        noflevel[i - alevel0][1] = i;
                    }
                    for (i = slevel0 + 1; i <= slevel; i++)
                    {
                        noflevel[i - slevel0 + alevel - alevel0][0] = i;
                        noflevel[i - slevel0 + alevel - alevel0][1] = alevel;
                    }
                    break;
                case 6: //MG4_a
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    if (ds >= da)
                    {
                        for (i = 0; i <= ds - da; i++)
                        {
                            noflevel[i][0] = i + slevel0;
                            noflevel[i][1] = alevel0;
                        }
                        for (i = 1; i <= da; i++)
                        {
                            noflevel[ds - da + 2 * i - 1][0] = slevel0 + ds - da + i;
                            noflevel[ds - da + 2 * i - 1][1] = alevel0 + i - 1;
                            noflevel[ds - da + 2 * i]    [0] = slevel0 + ds - da + i;
                            noflevel[ds - da + 2 * i]    [1] = alevel0 + i;
                        }
                    }
                    else
                    {
                        for (i = 0; i <= da - ds; i++)
                        {
                            noflevel[i][0] = slevel0;
                            noflevel[i][1] = i + alevel0;
                        }
                        for (i = 1; i <= ds; i++)
                        {
                            noflevel[da - ds + 2 * i - 1][0] = slevel0 + i;
                            noflevel[da - ds + 2 * i - 1][1] = alevel0 + da - ds + i - 1;
                            noflevel[da - ds + 2 * i][0] = slevel0 + i;
                            noflevel[da - ds + 2 * i][1] = alevel0 + da - ds + i;
                        }
                    }
                    break;
                case 7:  //MG4_s
                    for (i = 0; i <= level; i++)
                    { noflevel[i] = new int[2]; }
                    if (ds >= da)
                    {
                        for (i = 0; i <= ds - da; i++)
                        {
                            noflevel[i][0] = i + slevel0;
                            noflevel[i][1] = alevel0;
                        }
                        for (i = 1; i <= da; i++)
                        {
                            noflevel[ds - da + 2 * i - 1][0] = slevel0 + ds - da + i - 1;
                            noflevel[ds - da + 2 * i - 1][1] = alevel0 + i;
                            noflevel[ds - da + 2 * i][0] = slevel0 + ds - da + i;
                            noflevel[ds - da + 2 * i][1] = alevel0 + i;
                        }
                    }
                    else
                    {
                        for (i = 0; i <= da - ds; i++)
                        {
                            noflevel[i][0] = slevel0;
                            noflevel[i][1] = i + alevel0;
                        }
                        for (i = 1; i <= ds; i++)
                        {
                            noflevel[da - ds + 2 * i - 1][0] = slevel0 + i - 1;
                            noflevel[da - ds + 2 * i - 1][1] = alevel0 + da - ds + i;
                            noflevel[da - ds + 2 * i][0] = slevel0 + i;
                            noflevel[da - ds + 2 * i][1] = alevel0 + da - ds + i;
                        }
                    }
                    break;
            }

            // 3.2. malloc "mua", "mus", "flux", "RHS", "d" and "q"
            //      mua[level][nt][2]:absorption coefficient
            //      mus[level][nt][2]:scattering coefficient
            //      flux[level][ns][nt][2]: photon flux
            //      RHS[level][ns][nt][2]: source term
            //      d[level][ns][nt][2]: residual
            //      q[level][2][ns]: boundary source

                       

               

            for (i = slevel - 1; i >= 0; i--)
            {
                mua[i] = new double[smesh[i].nt][];
                mus[i] = new double[smesh[i].nt][];
                for (j = 0; j < smesh[i].nt; j++)
                {
                    mua[i][j] = new double[3];
                    mus[i][j] = new double[3];
                }
                 MultiGridCycle.FtoC_s2(smesh[i].nt, mua[i + 1], mua[i], smesh[i + 1].smap, smesh[i + 1].fc);
                 MultiGridCycle.FtoC_s2(smesh[i].nt,mus[i + 1], mus[i], smesh[i + 1].smap, smesh[i + 1].fc);
            }

                
            for (n = 0; n <= level; n++)
            {
                nt = smesh[noflevel[n][0]].nt;
                ns = amesh[noflevel[n][1]].ns;
                RHS[n] = new double[ns][][];
                for (i = 0; i < ns; i++)
                {
                    RHS[n][i] = new double[nt][];
                    for (j = 0; j < nt; j++)
                    {
                        RHS[n][i][j] = new double[3];
                        for (k = 0; k < 3; k++)
                        {
                            RHS[n][i][j][k] = 0;
                        }
                    }
                }
            }

            for (n = 0; n <= level; n++)
            {
                nt = smesh[noflevel[n][0]].nt;
                ns = amesh[noflevel[n][1]].ns;
                d[n] = new double[ns][][];
                for (i = 0; i < ns; i++)
                {
                    d[n][i] = new double[nt][];
                    for (j = 0; j < nt; j++)
                    {
                        d[n][i][j] = new double[3];
                        for (k = 0; k < 3; k++)
                        {
                            d[n][i][j][k] = 0;
                        }
                    }
                }
            }
            for (n = 0; n <= level; n++)
            {
                nt = smesh[noflevel[n][0]].nt;
                ns = amesh[noflevel[n][1]].ns;
                flux[n] = new double[ns][][];
                for (i = 0; i < ns; i++)
                {
                    flux[n][i] = new double[nt][];
                    for (j = 0; j < nt; j++)
                    {
                        flux[n][i][j] = new double[3];
                        for (k = 0; k < 3; k++)
                        {
                            flux[n][i][j][k] = 0;
                        }
                    }
                }
            }

           

            for (n = 0; n <= level; n++)
            {
                ne = smesh[noflevel[n][0]].ne;
                ns = amesh[noflevel[n][1]].ns;
                q[n] = new double[ns][][];
                for (i = 0; i < ns; i++)
                {
                    q[n][i] = new double [ne][];
                    for (j = 0; j < ne; j++)
                    {
                        q[n][i][j] = new double[2];
                        for (k = 0; k < 2; k++)
                        { q[n][i][j][k] = 0; }
                    }
                }
            }
            


            // 3.3. compute "b"
            //      For the data structure of "b", see "boundarycoupling".
            if (!vacuum)// we need "b" only in the presence of refraction index mismatch at the domain boundary.
            {
                for (i = 0; i <= level; i++)
                {
                    ne = smesh[noflevel[i][0]].ne;
                    ns = amesh[noflevel[i][1]].ns;

                    b[i].ri = new int[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].ri[j] = new int [ns][];
                        for (k = 0; k < ns; k++)
                            b[i].ri[j][k] = new int[2];
                    }

                    b[i].si = new int[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].si[j] = new int[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].si[j][k] = new int[2];
                    }

                    b[i].ro = new int[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].ro[j] = new int[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].ro[j][k] = new int[2];
                    }
                                        

                    b[i].so = new int[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].so[j] = new int[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].so[j][k] = new int[2];
                    }

                    b[i].ri2 = new double[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].ri2[j] = new double[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].ri2[j][k] = new double[2];
                    }

                    b[i].ro2 = new double[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].ro2[j] = new double[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].ro2[j][k] = new double[2];
                    }

                    b[i].si2 = new double[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].si2[j] = new double[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].si2[j][k] = new double[2];
                    }

                    b[i].so2 = new double[ne][][];
                    for (j = 0; j < ne; j++)
                    {
                        b[i].so2[j] = new double[ns][];
                        for (k = 0; k < ns; k++)
                            b[i].so2[j][k] = new double[2];
                    }

                     MultiGridCycle.BoundReflection(ns, amesh[noflevel[i][1]].a, smesh[noflevel[i][0]], index_i, index_o, b[i]);
                }
            }
        stop: ;
        }
    }
}