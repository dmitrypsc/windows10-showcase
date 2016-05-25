#!groovy
def version = "1.0." + env.BUILD_NUMBER + (env.BRANCH_NAME=="master"?"":"-beta");
def versionVSIX = "1.0." + env.BUILD_NUMBER;


node ('Windows') {

try {
	stage 'checkout'
    checkout scm

	stage 'nuget restore'
	bat '"C:\\Program Files (x86)\\NuGet\\Visual Studio 2015\\nuget.exe" restore SensorbergShowcase.sln'

	def msbuild = tool 'Main';

	stage 'build arm' 
	bat "\"${msbuild}\" /t:Clean,Build /p:Platform=ARM SensorbergShowcase.sln"

	stage 'build x64'
	bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x64 SensorbergShowcase.sln"

	stage 'build x86'
	bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x86 SensorbergShowcase.sln"

	def sub = env.JOB_NAME+' - Build '+env.BUILD_NUMBER+' - '+currentBuild.result
		emailext body: currentBuild, subject: sub , to: '$DEFAULT_RECIPIENTS'
}
catch(e) {
    node {
		def sub = env.JOB_NAME+' - Build '+env.BUILD_NUMBER+' - '+currentBuild.result
		emailext body: currentBuild, subject: sub , to: '$DEFAULT_RECIPIENTS'
    }
    throw e
}

}
