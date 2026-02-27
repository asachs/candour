using 'main.bicep'

param location = readEnvironmentVariable('CANDOUR_LOCATION', 'westeurope')
param environmentName = readEnvironmentVariable('CANDOUR_ENV', 'prod')
param apiKey = readEnvironmentVariable('CANDOUR_API_KEY', '')
param entraIdClientId = readEnvironmentVariable('CANDOUR_ENTRA_CLIENT_ID', '')
param adminEmails = split(readEnvironmentVariable('CANDOUR_ADMIN_EMAILS', ''), ';')
param tags = {
  owner: readEnvironmentVariable('CANDOUR_OWNER_ALIAS', '')
  ResourceOwnerAlias: readEnvironmentVariable('CANDOUR_OWNER_ALIAS', '')
  'owner-email': readEnvironmentVariable('CANDOUR_OWNER_EMAIL', '')
}
