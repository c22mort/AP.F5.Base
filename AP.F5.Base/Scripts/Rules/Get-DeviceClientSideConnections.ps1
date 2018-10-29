#==================================================================================
# Script: 	Get-DeviceClientSideConnections.ps1
# Date:		11/10/18
# Author: 	Andi Patrick
# Purpose:	Gets Device Failover State via SNMP returns as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddress,$DevicePort,$DeviceCommunity,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-DeviceClientSideConnections.ps1'
$EVENT_LEVEL_ERROR 		= 1
$VENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED				= 4601
$SCRIPT_PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT				= 4603
$SCRIPT_ENDED				= 4604
$SCRIPT_ERROR				= 4605

#==================================================================================
# Sub:		LogDebugEvent
# Purpose:	Logs an informational event to the Operations Manager event log
#			only if Debug argument is true
#==================================================================================
function Log-DebugEvent
{
	param($eventNo,$message)

	$message = "`n" + $message
	if ($Debug -eq $true)
	{
		$api.LogScriptEvent($SCRIPT_NAME,$eventNo,$EVENT_LEVEL_INFO,$message)
	}
}

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'

# Log Startup Message
Log-DebugEvent $SCRIPT_STARTED "Script Started."

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )

# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2

# Set Walk Mode to WithinSubtree
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree

# OIDs used
# bigipTrafficMgmt.bigipSystem.sysGlobals.sysGlobalStats.sysGlobalStat.sysStatClientMaxConns5m
[string]$sysStatClientMaxConns5m = ".1.3.6.1.4.1.3375.2.1.1.2.1.91.0"

Try 
{

	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($DeviceAddress)
	$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePort)

	# Create List for SNMP Get
	$vList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	$oidClient = New-Object Lextm.SharpSnmpLib.ObjectIdentifier ($sysStatClientMaxConns5m)
	$vList.Add($oidClient)

	Try {
		# Get SNMP Results
		$results = [Lextm.SharpSnmpLib.Messaging.Messenger]::Get($ver, $svr, $DeviceCommunity, $vList, 3000)
		# As Long as we have one result we can continue
		if ($Results.Count -gt 0) {
			[int]$Connections = $results[0].Data.ToString()
			#Create a property bag.
			$bag = $api.CreatePropertyBag()
			$bag.AddValue("Connections", [int]$Connections)
			[string] $message = "Created Property bag for $DeviceAddress`r`n" 	+ "Client-Side Connections: "  + $Connections + "`r`n"
			Log-DebugEvent $SCRIPT_PROPERTYBAG_CREATED $message
			#$api.Return($bag)
			$bag		
		}
	} Catch {
		$message = "SNMP Server : $DeviceAddress" + "`r`nSNMP Port : " + $DevicePort + "`r`nSNMP Community : " + $DeviceCommunity + "`r`nSharpSnmp Location : " + $SharpSnmpLocation + "`r`nError : $_"
		Log-DebugEvent $SCRIPT_ERROR $message
	}

}
Catch
{
	$message = "SNMP Server : $DeviceAddress" + "`r`nSNMP Port : " + $DevicePort + "`r`nSNMP Community : " + $DeviceCommunity + "`r`nSharpSnmp Location : " + $SharpSnmpLocation + "`r`nError : $_"
	Log-DebugEvent $SCRIPT_ERROR $message
}

# Get End Time For Script
$EndTime = (GET-DATE)
$TimeTaken = NEW-TIMESPAN -Start $StartTime -End $EndTime
$Seconds = [math]::Round($TimeTaken.TotalSeconds, 2)
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED "Script Finished. Took $Seconds Seconds to Complete!"