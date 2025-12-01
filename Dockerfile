FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Foreløpig enkel Dockerfile slik at CI-bygget lykkes.
# Når Svipp.Api-prosjektet er opprettet kan denne erstattes
# med en multi-stage build som publiserer og kjører API-et.

CMD ["bash", "-c", "echo Svipp API container - applikasjonen er ikke bygget enda. Oppdater Dockerfile når Svipp.Api er på plass. && sleep 3600"]


