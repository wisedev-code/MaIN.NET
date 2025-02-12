param(
    [Parameter(Position=0)]
    [string]$command,
    
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$arguments
)

# Set script root as a global variable for other scripts to use
$global:MCLI_ROOT = $PSScriptRoot

function Show-Usage {
    Write-Host @"
MaIN CLI (mcli) - Command Line Interface for MaIN

Usage:
    mcli <command> [options]

Commands:
    start-demo       Start all services (API, image generation, and download models)
    api         Start only the API service
    image-gen   Start only the image generation service
    model       Download and manage models
    help        Show this help message
    uninstall   Uninstall mcli

Options for 'start':
    --hard           Hard cleanup of containers
    --no-api         Skip starting the API
    --no-models      Skip model downloads
    --no-image-gen   Skip image generation
    --models=<list>  Specify comma-separated list of models to download

Options for 'api':
    --hard           Hard cleanup of containers

Options for 'model':
    download <name>   Download a specific model
    list             List available models
    update           Update all installed models

Examples:
    mcli start-demo
    mcli start-demo --no-image-gen
    mcli api --hard
    mcli model download llama2-7b
    mcli help
"@
}

function Show-CommandHelp {
    param([string]$cmd)
    
    switch ($cmd) {
        "start-demo" { 
            Write-Host @"
mcli start-demo - Start all MaIN services

Usage:
    mcli start [options]

Options:
    --hard           Perform hard cleanup of containers before starting
    --no-api         Skip starting the API
    --no-models      Skip model downloads
    --no-image-gen   Skip image generation
    --models=<list>  Specify comma-separated list of models to download

Examples:
    mcli start-demo
    mcli start-demo --no-image-gen
    mcli start-demo --models=llama3.2:3b,gemma2:2b
"@
        }
        "api" {
            Write-Host @"
mcli api - Start the MaIN API service

Usage:
    mcli api [options]

Options:
    --hard    Perform hard cleanup of containers before starting

Examples:
    mcli api
    mcli api --hard
"@
        }
        "image-gen" {
            Write-Host @"
mcli image-gen - Start the image generation service

Usage:
    mcli image-gen

Examples:
    mcli image-gen
"@
        }
        "model" {
            Write-Host @"
mcli model - Manage MaIN models

Usage:
    mcli model <subcommand>

Subcommands:
    download <name>   Download a specific model
    list             List available models
    update           Update all installed models

Examples:
    mcli model download llama2-7b
    mcli model list
    mcli model update
"@
        }
        default {
            Show-Usage
        }
    }
}

# Handle commands
switch ($command) {
    "start-demo" {
        & "$PSScriptRoot\start.ps1" $arguments
    }
    "api" {
        & "$PSScriptRoot\start-api.ps1" $arguments
    }
    "image-gen" {
        & "$PSScriptRoot\start-image-gen.ps1" $arguments
    }
    "model" {
        $subcommand = $arguments[0]
        $modelArgs = $arguments[1..($arguments.Length-1)]
        
        switch ($subcommand) {
            "download" {
                & "$PSScriptRoot\download-models.ps1" $modelArgs[0]
            }
            "list" {
                Write-Host "Available models:"
                Get-Content "$PSScriptRoot\models_map.txt" | Where-Object { $_ -notmatch '^\s*#' -and $_ -notmatch '^\s*$' }
            }
            "update" {
                Write-Host "Updating all installed models..."
                & "$PSScriptRoot\download-models.ps1"
            }
            default {
                Show-CommandHelp "model"
            }
        }
    }
    "uninstall" {
        & "$PSScriptRoot\uninstall.ps1"
    }
    "help" {
        if ($arguments.Count -gt 0) {
            Show-CommandHelp $arguments[0]
        } else {
            Show-Usage
        }
    }
    default {
        Show-Usage
        if ($command) {
            Write-Host "`nError: Unknown command '$command'" -ForegroundColor Red
        }
    }
}