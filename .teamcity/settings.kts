import jetbrains.buildServer.configs.kotlin.v2018_1.*
import jetbrains.buildServer.configs.kotlin.v2018_1.Project
import jetbrains.buildServer.configs.kotlin.v2018_1.buildFeatures.*
import jetbrains.buildServer.configs.kotlin.v2018_1.buildSteps.*
import jetbrains.buildServer.configs.kotlin.v2018_1.triggers.*
import jetbrains.buildServer.configs.kotlin.v2018_1.vcs.*
import jetbrains.buildServer.configs.kotlin.v2018_1.ui.*
import jetbrains.buildServer.configs.kotlin.v2018_1.vcs.GitVcsRoot


version = "2018.2"

project {
    description = "https://github.com/servicetitan/Stl"
    vcsRoot(VcsRoot)

    buildType(Compile)
    buildType(Tests)
    buildType(Publish)

    buildTypesOrder = arrayListOf(Compile, Tests, Publish)

    params {
        text (
            "env.NUGET_FEED",
            label = "NugetFeed",
            description = "Nuget feed url key for publish nupkg, default is https://www.myget.org/F/servicetitan/",
            value = "https://www.myget.org/F/servicetitan/",
            allowEmpty = true,
            display = ParameterDisplay.NORMAL)
        password("env.GITHUB_AUTH_TOKEN", "credentialsJSON:9ebdf487-5e00-43e0-8df7-828468702cd8", label = "github token", description = "OAuth Token for ReportGenerator SourceLink support", display = ParameterDisplay.HIDDEN)
        password("env.DATADOG_API_KEY", "credentialsJSON:255b62dd-e8a0-498f-8049-9b206df27490", label = "DataDog api key", description = "datadog api for publish coverage report", display = ParameterDisplay.HIDDEN)
        password("env.NUGET_API_KEY", "credentialsJSON:59d3e58d-abd1-4452-a409-1f2a23872aa2", label = "Nuget api key", description = "Myget api key for publish", display = ParameterDisplay.HIDDEN)
        param(
            "teamcity.runner.commandline.stdstreams.encoding",
            "UTF-8"
        )
    }
    // ReportTab feature is defined in upper project
}

object VcsRoot : GitVcsRoot({
    id("VcsRoot")
    name = "https://github.com/servicetitan/Stl"
    url = "https://github.com/servicetitan/Stl.git"
    branch = "refs/heads/master"
    pollInterval = 60
    branchSpec = """
        +:refs/heads/*
        +:pull/*
    """.trimIndent()
    authMethod = password {
        userName = "ServiceTitanTeamCity"
        password = "credentialsJSON:be7b2ad3-34a5-498a-8171-ac8addd775c7"
    }
})

object Compile : BuildType({
    name = "Compile"
    vcs {
        root(VcsRoot)
        cleanCheckout = true
    }
    artifactRules = """
        artifacts/**/*=>artifacts.zip
        -:artifacts/samples/**/*=>artifacts.zip
        -:artifacts/tools/**/*=>artifacts.zip
    """.trimIndent()
    cleanup {
        artifacts(days = 15)
        history(days = 30)
    }
    failureConditions {
        executionTimeoutMin = 20
        errorMessage = true
    }
    steps {
        script {
            scriptContent = "dotnet run --project build/_build.csproj -- rebuild"
        }
    }
    requirements {
        equals("env.OS", "Windows_NT")
        exists("DotNetCoreSDK3.1.201_Path")
    }
    triggers {
        vcs {
            quietPeriodMode = VcsTrigger.QuietPeriodMode.USE_DEFAULT
            branchFilter = """
                +:master
                +:pull/*
            """.trimIndent()
            perCheckinTriggering = true
            groupCheckinsByCommitter = true
            enableQueueOptimization = true
        }
    }
})

object Tests : BuildType({
    name = "Tests"
    description = "Run unit tests"
    vcs {
        root(VcsRoot)
    }
    artifactRules = """
        artifacts/tests/output/**/*=>report.zip
    """.trimIndent()
    cleanup {
        artifacts(days = 15)
        history(days = 30)
    }
    failureConditions {
        executionTimeoutMin = 25
        errorMessage = true
    }
    steps {
        script {
            scriptContent = "dotnet run --project build/_build.csproj -- coverage publish-datadog --datadog-team backend-platform --datadog-name platform-dotnet"
        }
    }
    dependencies {
        snapshot(Compile) {
            onDependencyFailure = FailureAction.FAIL_TO_START
            onDependencyCancel = FailureAction.CANCEL
        }
        artifacts(Compile) {
            buildRule = sameChainOrLastFinished()
            artifactRules = "artifacts.zip!**=>artifacts"
        }
    }
    triggers {
        finishBuildTrigger {
            buildType = "${Compile.id}"
            branchFilter = """
                +:<default>
                +:*
            """.trimIndent()
        }
    }
    requirements {
        equals("env.OS", "Windows_NT")
        exists("DotNetCoreSDK3.1.201_Path")
    }
    features {
        pullRequests {
            provider = github {
                authType = vcsRoot()
                filterAuthorRole = PullRequests.GitHubRoleFilter.MEMBER
            }
        }
        commitStatusPublisher {
            vcsRootExtId = "${DslContext.settingsRoot.id}"
            publisher = github {
                githubUrl = "https://api.github.com"
                authType = personalToken {
                    /* sistr token */
                    token = "credentialsJSON:4e31199a-eb9b-4c62-89d1-8807df1fbb44"
                }
            }
        }
    }
})

object Publish : BuildType({
    name = "Publish"
    description = "Publish nupkg to myget and make coverage report"
    vcs {
        root(VcsRoot)
    }
    artifactRules = """
        artifacts/nupkg/**/*=>nupkg.zip
    """.trimIndent()
    cleanup {
        artifacts(days = 30)
        history(days = 30)
    }
    failureConditions {
        executionTimeoutMin = 10
        errorMessage = true
    }
    steps {
        script {
            scriptContent = "dotnet run --project build/_build.csproj -- restore publish publish-datadog --datadog-team backend-platform --datadog-name platform-dotnet"
        }
    }
    dependencies {
        snapshot(Compile) {
            onDependencyFailure = FailureAction.FAIL_TO_START
            onDependencyCancel = FailureAction.CANCEL
        }
        artifacts(Tests) {
            buildRule = sameChainOrLastFinished()
            artifactRules = "report.zip!**=>artifacts/tests/output"
        }
        artifacts(Compile) {
            buildRule = sameChainOrLastFinished()
            artifactRules = "artifacts.zip!**=>artifacts"
        }
    }
    requirements {
        equals("env.OS", "Windows_NT")
        exists("DotNetCoreSDK3.1.201_Path")
    }
})
