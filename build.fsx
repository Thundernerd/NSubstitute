#r @"ThirdParty\FAKE\FAKE.Core\tools\FakeLib.dll"
open Fake 
open System

let EXPERIMENTAL_TARGETS = []

let buildNumber = "1.8.0.0"

let buildMode = getBuildParamOrDefault "buildMode" "Debug"

let OUTPUT_PATH = "./Output"

Target "Clean" (fun _ ->
    CleanDirs [ OUTPUT_PATH ]
)

Target "BuildSolution" (fun _ ->
  let seqBuild = Seq.map (fun config -> 
      MSBuild null "Build" ["Configuration", config] ["./Source/NSubstitute.2010.sln"]
          |> Log "Build: " )

  Seq.last (seqBuild [ "NET35-"+buildMode ; "NET40-"+buildMode ])
)

let outputDir = String.Format("{0}/{1}/", OUTPUT_PATH, buildMode)
let testDlls = !! (outputDir + "**/*Specs.dll")

Target "Test" (fun _ ->
    testDlls
        |>  NUnit (fun p ->
            {p with
                DisableShadowCopy = true;
                Framework = "net-4.0";
                ExcludeCategory = "Pending";
                OutputFile = outputDir + "TestResults.xml"}) // TODO: different file name based on path
)

let outputBasePath =  String.Format("{0}/{1}/", OUTPUT_PATH, buildMode);
let workingDir = String.Format("{0}package/", outputBasePath)

let net35binary = String.Format("{0}NET35/NSubstitute/NSubstitute.dll", outputDir)
let net40binary = String.Format("{0}NET40/NSubstitute/NSubstitute.dll", outputDir)
let net35binariesDir = String.Format("{0}lib/net35", workingDir)
let net40binariesDir = String.Format("{0}lib/net40", workingDir)

Target "NuGet" (fun _ ->
    //CreateDir workingDir
    CreateDir net35binariesDir
    CreateDir net40binariesDir

    // Copy binaries into lib path
    CopyFile net35binariesDir net35binary
    CopyFile net40binariesDir net40binary

    NuGet (fun p ->
        {p with
            OutputPath = outputBasePath
            WorkingDir = workingDir
            Version = buildNumber
             }) "Build/NSubstitute.nuspec"
)

Target "Default" DoNothing

"Clean"
   ==> "BuildSolution"
   ==> "Test"
   ==> "NuGet"
   ==> "Default"

RunTargetOrDefault "Default"