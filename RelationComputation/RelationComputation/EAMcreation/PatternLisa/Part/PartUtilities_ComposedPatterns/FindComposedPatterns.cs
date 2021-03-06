﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyRetrieval.PatternLisa.ClassesOfObjects;
using AssemblyRetrieval.PatternLisa.GeometricUtilities;
using SolidWorks.Interop.sldworks;
using Functions = AssemblyRetrieval.PatternLisa.Part.PathCreation_Part.Functions;

namespace AssemblyRetrieval.PatternLisa.Part.PartUtilities_ComposedPatterns
{
    public partial class GeometryAnalysis
    {
      
        public static void FindComposedPatterns(List<MyGroupingSurfaceForPatterns> listOfGroupingSurfaceForPatterns, 
            out List<MyComposedPattern> listOfOutputComposedPattern,
            out List<MyComposedPattern> listOfOutputComposedPatternTwo, ModelDoc2 SwModel,
            SldWorks mySwApplication, ref StringBuilder fileOutput)

        {
            
            var toleranceOk = true;
            var listOfComposedPattern = new List<MyComposedPattern>();
            var listOfComposedPatternTwo = new List<MyComposedPattern>();

            //For each MyGroupingSurfaceForPatterns 
            while (listOfGroupingSurfaceForPatterns.Count > 0 && toleranceOk)
            {
                var currentGroupingSurfaceForPatterns = new MyGroupingSurfaceForPatterns(
                    listOfGroupingSurfaceForPatterns[0].groupingSurface, 
                    listOfGroupingSurfaceForPatterns[0].listOfPatternsLine,
                    listOfGroupingSurfaceForPatterns[0].listOfPatternsCircum);
                listOfGroupingSurfaceForPatterns.RemoveAt(0);
               
                fileOutput.AppendLine("");
                fileOutput.AppendLine("NUOVA SURFACE OF TYPE: " + currentGroupingSurfaceForPatterns.groupingSurface.Identity());
                fileOutput.AppendLine("(Al momento sono rimaste ancora " + listOfGroupingSurfaceForPatterns.Count +
                        " superfici (oltre questa).)");

                // >>>>>>> LINEAR CASE:
                //first I group patterns by same length and same distance:
                var listOfListsOfCoherentPatternsLine = GroupFoundPatternsOfTypeLine(
                    currentGroupingSurfaceForPatterns.listOfPatternsLine, mySwApplication);
                if (listOfListsOfCoherentPatternsLine.Count == 0)
                {
                }

                //Then I group coherent patterns in subgroups of parallel patterns:
                foreach (var list in listOfListsOfCoherentPatternsLine)
                {
                    //Grouping in lists of parallel patterns
                    var listOfListsOfParallelPatterns = GroupPatternsInParallel(list);
                    var numOfListsOfParallelPatterns = listOfListsOfParallelPatterns.Count;

                    if (list.Count == 2)
                    {          
                        //I verify if a composed pattern with these 2 patterns has already been created
                        //in another GS:
                        if (ComposedPatternOfLength2AlreadyExists(listOfComposedPatternTwo, list) == false)
                        {
                            //if a composed pattern does not exist yet AND
                            //the 2 patterns are not parallel, I verify if it is REFLECTION:
                            if (numOfListsOfParallelPatterns == 0)
                            {
                                if (IsReflectionTwoPatterns(list[0], list[1]))
                                {
                                    var typeOfNewComposedPattern = "Composed REFLECTION of length 2";
                                    BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                        ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                        ref listOfGroupingSurfaceForPatterns, list);
                                }

                            }
                            else
                            //if a composed pattern does not exist yet AND
                            //the 2 patterns are parallel, I verify if it is TRANSLARION:
                            {
                                if (IsTranslationTwoPatterns(list[0], list[1]))
                                {
                                    var typeOfNewComposedPattern = "Composed TRANSLATION of length 2";
                                    BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                        ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                        ref listOfGroupingSurfaceForPatterns, list);
                                }
                                else
                                {
                                    if (IsReflectionTwoPatterns(list[0], list[1]))
                                    {
                                        var typeOfNewComposedPattern = "Composed REFLECTION of length 2";
                                        BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                            ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                            ref listOfGroupingSurfaceForPatterns, list);
                                    }
                                 
                                }

                            }
                        }
                        
                    }
                    else
                    {

                        foreach (var listOfParallelPatterns in listOfListsOfParallelPatterns)
                        {
                            if (listOfParallelPatterns.Count == 2)
                            {
                                //I verify if a composed pattern with these 2 patterns has already been created
                                //in another GS:
                                if (ComposedPatternOfLength2AlreadyExists(listOfComposedPatternTwo, list))
                                {
                                }
                                else
                                {
                                    if (IsTranslationTwoPatterns(listOfParallelPatterns[0], listOfParallelPatterns[1]))
                                    {
                                        var typeOfNewComposedPattern = "Composed TRANSLATION of length 2";
                                        BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                            ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                            ref listOfGroupingSurfaceForPatterns, listOfParallelPatterns);
                                    }
                                    else
                                    {
                                        if (IsReflectionTwoPatterns(list[0], list[1]))
                                        {
                                            var typeOfNewComposedPattern = "Composed REFLECTION of length 2";
                                            BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                                ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                                ref listOfGroupingSurfaceForPatterns, list);
                                        }
                                        
                                    }
                                }
                            }
                            else //(listOfParallelPatterns.Count > 2)
                            {
                                var listOfPatternCentroids = listOfParallelPatterns.Select(pattern => pattern.patternCentroid).ToList();
                                fileOutput.AppendLine("");
                                fileOutput.AppendLine("        >>>> CREATION OF ADJECENCY MATRICES FOR COMPOSED PATTERNS:");
                                var listOfMyMatrAdj = Functions.CreateMatrAdj(listOfPatternCentroids, ref fileOutput);

                                var maxPath = false; //it is TRUE if the maximum Pattern is found, FALSE otherwise.
                                while (listOfMyMatrAdj.Count > 0 && maxPath == false && toleranceOk)
                                {
                                    var currentMatrAdj = new MyMatrAdj(listOfMyMatrAdj[0].d, listOfMyMatrAdj[0].matr,
                                        listOfMyMatrAdj[0].nOccur);
                                    listOfMyMatrAdj.Remove(listOfMyMatrAdj[0]);
                                    //NOTA: forse la mia MatrAdj non deve essere rimossa ma conservata,
                                    //soprattutto nel caso in cui si presenta onlyShortPath = true
                                    //(non avrebbe senso cancellarla, ma conservarla per la ricerca di path
                                    //di 2 RE).
                                    List<MyPathOfPoints> listOfPathsOfCentroids;
                                    bool onlyShortPaths;
                                    maxPath = PathCreation_Part_ComposedPatterns.Functions.FindPaths_ComposedPatterns(currentMatrAdj,
                                        listOfParallelPatterns,
                                        ref fileOutput, out listOfPathsOfCentroids, out onlyShortPaths, ref toleranceOk,
                                        ref listOfMyMatrAdj,
                                        ref listOfGroupingSurfaceForPatterns, ref listOfComposedPattern,
                                        ref listOfComposedPatternTwo, mySwApplication);
                                    //fileOutput.AppendLine("listOfPathOfCentroids.Count = " + listOfPathOfCentroids.Count, nameFile);

                                    if (toleranceOk)
                                    {
                                        if (listOfPathsOfCentroids != null)
                                        {
                                            if (maxPath == false)
                                            {
                                                if (onlyShortPaths == false)
                                                {
                                                    GetComposedPatternsFromListOfPathsLine(listOfPathsOfCentroids,
                                                        listOfParallelPatterns,
                                                        ref listOfMyMatrAdj, ref listOfGroupingSurfaceForPatterns,
                                                        ref listOfComposedPattern, ref listOfComposedPatternTwo, mySwApplication, ref fileOutput);
                                                }
                                                else
                                                {
                                                    //non faccio niente e li rimetto in gioco per 
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //Now coherent linear patterns that have not been set in a composed pattern yet
                        //are examined to see if they consitute any rotation composed pattern:
                        
                        list.RemoveAll(
                            pattern =>
                                listOfComposedPattern.FindIndex(
                                    composedPattern =>
                                        composedPattern.listOfMyPattern.FindIndex(
                                            patternInComposedPattern =>
                                                patternInComposedPattern.idMyPattern == pattern.idMyPattern)!= -1)!=-1);
                        if (list.Count != 0)
                        {
                            if (list.Count == 2)
                            {
                                //  >>> POSSIBLE REFLECTION CAN EXIST

                                //I verify if a composed pattern with these 2 patterns has already been created
                                //in another GS:
                                if (ComposedPatternOfLength2AlreadyExists(listOfComposedPatternTwo, list))
                                {
                                }
                                else
                                {
                                    if (IsReflectionTwoPatterns(list[0], list[1]))
                                    {
                        
                                        var typeOfNewComposedPattern = "Composed REFLECTION of length 2";
                                        BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                            ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                            ref listOfGroupingSurfaceForPatterns, list);
                                    }
                                    
                                }
                            }
                            else
                            {
                                var listOfPatternCentroids1 = list.Select(pattern => pattern.patternCentroid).ToList();
                                fileOutput.AppendLine("");
                                fileOutput.AppendLine("        >>>> CREATION OF ADJECENCY MATRICES FOR COMPOSED PATTERNS:");
                                var listOfMyMatrAdj1 = Functions.CreateMatrAdj(listOfPatternCentroids1, ref fileOutput);

                                var maxPath = false; //it is TRUE if the maximum Pattern is found, FALSE otherwise.
                                while (listOfMyMatrAdj1.Count > 0 && maxPath == false && toleranceOk)
                                {
                                    var currentMatrAdj = new MyMatrAdj(listOfMyMatrAdj1[0].d, listOfMyMatrAdj1[0].matr,
                                        listOfMyMatrAdj1[0].nOccur);
                                    listOfMyMatrAdj1.Remove(listOfMyMatrAdj1[0]);
                                    //NOTA: forse la mia MatrAdj non deve essere rimossa ma conservata,
                                    //soprattutto nel caso in cui si presenta onlyShortPath = true
                                    //(non avrebbe senso cancellarla, ma conservarla per la ricerca di path
                                    //di 2 RE).
                                    List<MyPathOfPoints> listOfPathsOfCentroids1;
                                    bool onlyShortPaths1;

                                    var maxPath1 = PathCreation_Part_ComposedPatterns.Functions.FindPaths_ComposedPatterns(currentMatrAdj,
                                        list, ref fileOutput, out listOfPathsOfCentroids1, out onlyShortPaths1,
                                        ref toleranceOk, ref listOfMyMatrAdj1, ref listOfGroupingSurfaceForPatterns, 
                                        ref listOfComposedPattern, ref listOfComposedPatternTwo, mySwApplication);

                                    if (toleranceOk)
                                    {
                                        if (listOfPathsOfCentroids1 != null)
                                        {
                                            //I ignore every linear path (I look for composed rotational patterns now):
                                            var listOfCircularPaths =
                                                listOfPathsOfCentroids1.FindAll(
                                                    path =>
                                                        path.pathGeometricObject.GetType() == typeof (MyCircumForPath))
                                                    .ToList();

                                            if (maxPath1 == false)
                                            {
                                                if (onlyShortPaths1 == false)
                                                {
                                                    GetComposedPatternsFromListOfPathsLine(listOfCircularPaths,
                                                        list, ref listOfMyMatrAdj1, ref listOfGroupingSurfaceForPatterns,
                                                        ref listOfComposedPattern, ref listOfComposedPatternTwo, mySwApplication, ref fileOutput);
                                                }
                                                else
                                                {
                                                    //non faccio niente e li rimetto in gioco per 
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }
                       
                    }

                }
                
                // >>>>>>> CIRCULAR CASE:
                #region da rivedere
                //first I group patterns by same length and same distance:
                var listOfListsOfCoherentPatternsCircum = GroupFoundPatternsOfTypeCircum(
                    currentGroupingSurfaceForPatterns.listOfPatternsCircum, mySwApplication);
                
                //Then I group coherent patterns in subgroups of patterns:
                foreach (var list in listOfListsOfCoherentPatternsCircum)
                {
                    var listOfListsOfPatternsWithSameCenterAndPlane = GroupPatternsWithSameCenterPlane(list);
                    var numOfListsOfParallelPatterns = listOfListsOfPatternsWithSameCenterAndPlane.Count;
                    //Grouping in lists of parallel patterns
                    foreach (var listOfPatternsWithSameCenterAndPlane in listOfListsOfPatternsWithSameCenterAndPlane)
                    {
                        if (listOfPatternsWithSameCenterAndPlane.Count == 2)
                        {
                            if (IsTranslationTwoPatterns(listOfPatternsWithSameCenterAndPlane[0], listOfPatternsWithSameCenterAndPlane[1]))
                            {
                                var typeOfNewComposedPattern = "Composed ROTATIONAL (same center) of length 2";
                                BuildNewComposedPatternOfLength2(fileOutput, typeOfNewComposedPattern,
                                    ref listOfComposedPattern, ref listOfComposedPatternTwo,
                                    ref listOfGroupingSurfaceForPatterns, listOfPatternsWithSameCenterAndPlane);
                            }
                            
                        }
                        else //listOfPatternsWithSameCenterAndPlane.Count > 2
                        {
                        //    var listOfPatternCentroids =
                        //        listOfPatternsWithSameCenterAndPlane.Select(pattern => pattern.patternCentroid).ToList();
                            //    KLdebug.Print("", nameFile);
                            //    KLdebug.Print("        >>>> CREATION OF ADJECENCY MATRICES FOR COMPOSED PATTERNS:", nameFile);
                        //    var listOfMyMatrAdj = Functions.CreateMatrAdj(listOfPatternCentroids, ref fileOutput);

                        //    var indOfMatr = 0;
                        //    List<MyPathOfPoints> listOfPathsOfCentroids;
                        //    bool onlyShortPaths;


                        //    //////////////
                        //    var maxPath = Functions.FindPaths_ComposedPatterns(listOfMyMatrAdj[indOfMatr], listOfPatternsWithSameCenterAndPlane,
                        //            ref fileOutput, out listOfPathsOfCentroids, out onlyShortPaths, ref toleranceOk, ref listOfMyMatrAdj,
                        //            ref listOfGroupingSurfaceForPatterns, ref listOfComposedPattern, ref listOfComposedPatternTwo);

                        //    if (toleranceOk)
                        //    {
                        //        if (maxPath == false)
                        //        {
                        //            if (onlyShortPaths == false)
                        //            {
                        //                GetComposedPatternsFromListOfPathsLine(listOfPathsOfCentroids,
                        //                    listOfPatternsWithSameCenterAndPlane,
                        //                    ref listOfMyMatrAdj, ref listOfGroupingSurfaceForPatterns,
                        //                    ref listOfComposedPattern, ref listOfComposedPatternTwo);
                        //            }
                        //            else
                        //            {
                        //                //non faccio niente e li rimetto in gioco per 
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                            //        KLdebug.Print("===>>    TOLLERANZA NON SUFFICIENTEMENTE PICCOLA. TERMINATO.", nameFile);
                        //    }
                        }
                    }
                }

            }
                #endregion
            listOfOutputComposedPattern = listOfComposedPattern;
            listOfOutputComposedPatternTwo = listOfComposedPatternTwo;
        }

        public static bool ComposedPatternOfLength2AlreadyExists(List<MyComposedPattern> listOfComposedPatternTwo,
            List<MyPattern> listOfPatterns)
        {
            var firstPattern = listOfPatterns[0];
            var secondPattern = listOfPatterns[1];
            var indOfFound =
                listOfComposedPatternTwo.FindIndex(
                    composedPattern => composedPattern.listOfMyPattern.FindIndex(
                        pattern => pattern.idMyPattern == firstPattern.idMyPattern) != -1 &&
                        composedPattern.listOfMyPattern.FindIndex(
                        pattern => pattern.idMyPattern == secondPattern.idMyPattern) != -1);
            if (indOfFound == -1)
            {
                return false;
            }
            return true;
        }

        public static void BuildNewComposedPatternOfLength2(StringBuilder fileOutput, string typeOfNewComposedPattern, 
            ref List<MyComposedPattern> listOfComposedPattern, ref List<MyComposedPattern> listOfComposedPatternTwo,
            ref List<MyGroupingSurfaceForPatterns> listOfGroupingSurfaceForPatterns, List<MyPattern> listOfPatternsToConsider)
        {
            var listOfPathOfCentroids = new List<MyPathOfPoints>();
            var listOfMyMatrAdj = new List<MyMatrAdj>();
            var newListOfPatterns = new List<MyPattern>();
            newListOfPatterns.Add(listOfPatternsToConsider[0]);
            newListOfPatterns.Add(listOfPatternsToConsider[1]);
            var newComposedPatternGeomObject = FunctionsLC.LinePassingThrough(
                listOfPatternsToConsider[0].patternCentroid, listOfPatternsToConsider[1].patternCentroid);
            var newComposedPatternType = typeOfNewComposedPattern;
            var newComposedPattern = new MyComposedPattern(newListOfPatterns, 
                newComposedPatternGeomObject, newComposedPatternType);
            CheckAndUpdate_ComposedPatterns(newComposedPattern, ref listOfPathOfCentroids,
                listOfPatternsToConsider, ref listOfMyMatrAdj, ref listOfGroupingSurfaceForPatterns,
                ref listOfComposedPattern, ref listOfComposedPatternTwo);
        }       

        //   (NOT USED function)
        //This function verifies is two linear patterns are collinear (share the same line corresponding
        //to their path). It returns TRUE if they do, FALSE otherwise.
        public static bool AreCollinear(MyPattern first, MyPattern second)
        {
            var firstLine = (MyLine)first.pathOfMyPattern;
          
            if (second.listOfMyREOfMyPattern[0].centroid.Lieonline(firstLine) &&
                second.listOfMyREOfMyPattern[1].centroid.Lieonline(firstLine))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //This function takes as input a list of patterns with MyPathGeometricObject line,
        //it subdivides the patterns by same number of RE on the pattern, then by constant distance.
        public static List<List<MyPattern>> GroupFoundPatternsOfTypeLine(List<MyPattern> listOfPatternsOfTypeLine, SldWorks mySwApplication)
        {
            var nameFile = "ComposedPatterns.txt";
            var tolerance = Math.Pow(10, -4);
            var listOfListsOfPatterns = new List<List<MyPattern>>();
            foreach (var pattern in listOfPatternsOfTypeLine)
            {
             
                var indexOfFind = listOfListsOfPatterns.FindIndex(list => (list[0].listOfMyREOfMyPattern.Count ==
                    pattern.listOfMyREOfMyPattern.Count && Math.Abs(list[0].constStepOfMyPattern - pattern.constStepOfMyPattern)<tolerance));
                //var indexOfFind1 = listOfListsOfPatterns.FindIndex(list => (list[0].listOfMyREOfMyPattern.Count == 
                //    pattern.listOfMyREOfMyPattern.Count));
                //var indexOfFind2 = listOfListsOfPatterns.FindIndex(list => (Math.Abs(list[0].constStepOfMyPattern - pattern.constStepOfMyPattern) < tolerance));
                //KLdebug.Print("     trova indice lista con num elementi corrispondente: " + indexOfFind1, nameFile);
                //KLdebug.Print("     trova indice lista con num distanza corrispondente: " + indexOfFind2, nameFile);

                if (indexOfFind != -1)
                {
                    //The list referred to patterns with same RE number and same constant step already exists. I add it to the corresponding list:
                    listOfListsOfPatterns[indexOfFind].Add(pattern);
                }
                else
                {
                    //The list referred to patterns with same RE number and same constant step does not exist yet. I create it:
                    List<MyPattern> newListOfPatterns = new List<MyPattern> { pattern };
                    listOfListsOfPatterns.Add(newListOfPatterns);
                }
            }
            listOfListsOfPatterns.RemoveAll(list => list.Count < 2);
            return listOfListsOfPatterns;
        }

        //This function takes as input a list of patterns with MyPathGeometricObject circumference,
        //it subdivides the patterns by same number of RE on the pattern, then by angle.
        public static List<List<MyPattern>> GroupFoundPatternsOfTypeCircum(List<MyPattern> listOfPatternsOfTypeCircum, SldWorks mySwApplication)
        {
            var nameFile = "ComposedPatterns.txt";
            var tolerance = Math.Pow(10, -6);
            var listOfListsOfPatterns = new List<List<MyPattern>>();
            foreach (var pattern in listOfPatternsOfTypeCircum)
            {
                var indexOfFind = listOfListsOfPatterns.FindIndex(list => (list[0].listOfMyREOfMyPattern.Count ==
                                                                           pattern.listOfMyREOfMyPattern.Count && Math.Abs(list[0].angle - pattern.angle) < tolerance));
                if (indexOfFind != -1)
                {
                    //The list referred to patterns with same RE number and same constant step already exists. I add it to the corresponding list:
                    listOfListsOfPatterns[indexOfFind].Add(pattern);
                }
                else
                {
                    //The list referred to patterns with same RE number and same constant step does not exist yet. I create it:
                    List<MyPattern> newListOfPatterns = new List<MyPattern> { pattern };
                    listOfListsOfPatterns.Add(newListOfPatterns);
                }
            }
            listOfListsOfPatterns.RemoveAll(list => list.Count < 2);
            
            return listOfListsOfPatterns;
        }

        //This function subdivides a list of patterns circumference in patterns with same center and plane
        public static List<List<MyPattern>> GroupPatternsWithSameCenterPlane(List<MyPattern> listsOfCircumPatterns)
        {
            var nameFile = "ComposedPatterns.txt";

            var listOfListsOfPatternsWithSameCenterPlane = new List<List<MyPattern>>();
            foreach (var pattern in listsOfCircumPatterns)
            {
                var circumThisPattern = (MyCircumForPath)pattern.pathOfMyPattern;
                var commonCenter = circumThisPattern.circumcenter;
                var commonPlane = circumThisPattern.circumplane;

                var indexOfFind =
                    listOfListsOfPatternsWithSameCenterPlane.FindIndex(
                        list =>
                            ((MyCircumForPath) list[0].pathOfMyPattern).circumcenter.Equals(commonCenter) &&
                            ((MyCircumForPath) list[0].pathOfMyPattern).circumplane.Equals(commonPlane));
                if (indexOfFind != -1)
                {
                    //The list already exists. I add it to the corresponding list:
                    listOfListsOfPatternsWithSameCenterPlane[indexOfFind].Add(pattern);
                }
                else
                {
                    //The list does not exist yet. I create it:
                    List<MyPattern> newListOfPatterns = new List<MyPattern> { pattern };
                    listOfListsOfPatternsWithSameCenterPlane.Add(newListOfPatterns);
                }
            }
            listOfListsOfPatternsWithSameCenterPlane.RemoveAll(list => list.Count < 2);

            return listOfListsOfPatternsWithSameCenterPlane;
        }

        //This function subdivides a list of patterns line in patterns with parallel lines
        public static List<List<MyPattern>> GroupPatternsInParallel(List<MyPattern> listsOfLinePatterns)
        {
            var nameFile = "ComposedPatterns.txt";

            var listOfListsOfParallelPatterns = new List<List<MyPattern>>();
            foreach (var pattern in listsOfLinePatterns)
            {
                var lineThisPattern = (MyLine)pattern.pathOfMyPattern;
                var invertedDirectionThisPattern = new double[3];
                invertedDirectionThisPattern.SetValue(-lineThisPattern.direction[0],0);
                invertedDirectionThisPattern.SetValue(-lineThisPattern.direction[1],1);
                invertedDirectionThisPattern.SetValue(-lineThisPattern.direction[2],2);

                var indexOfFind = listOfListsOfParallelPatterns.FindIndex(list => 
                    (FunctionsLC.MyEqualsArray(((MyLine)list[0].pathOfMyPattern).direction, 
                    ((MyLine)pattern.pathOfMyPattern).direction) || FunctionsLC.MyEqualsArray(((MyLine)list[0].pathOfMyPattern).direction, 
                    invertedDirectionThisPattern)));
                if (indexOfFind != -1)
                {
                    var whatToWrite = string.Format("     AGGIORNATA LISTA di pattern ESISTENTE con direzione ({0},{1},{2})", 
                        ((MyLine)pattern.pathOfMyPattern).direction[0], ((MyLine)pattern.pathOfMyPattern).direction[1], 
                        ((MyLine)pattern.pathOfMyPattern).direction[2]);                        
                    //The list already exists. I add it to the corresponding list:
                    listOfListsOfParallelPatterns[indexOfFind].Add(pattern);
                }
                else
                {
                    //The list does not exist yet. I create it:
                    List<MyPattern> newListOfPatterns = new List<MyPattern> { pattern };
                    listOfListsOfParallelPatterns.Add(newListOfPatterns);
                    var whatToWrite = string.Format("     CREATA NUOVA LISTA di pattern linea paralleli con direzione ({0},{1},{2})",
                        ((MyLine)pattern.pathOfMyPattern).direction[0], ((MyLine)pattern.pathOfMyPattern).direction[1],
                        ((MyLine)pattern.pathOfMyPattern).direction[2]);
                }
            }

            listOfListsOfParallelPatterns.RemoveAll(list => list.Count < 2);
   
            return listOfListsOfParallelPatterns;
        }

    }
}
