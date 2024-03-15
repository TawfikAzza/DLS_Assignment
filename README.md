# DLS_Assignment
This README file contains setup instructions and the following guides:
- Guide for Tagging and Pushing Service Images to Docker Hub
- Docker Swarm Setup Guide for Project DLS_Assignment

# Setup
Create 2 networks:
- docker network create intranet
- docker network create --driver=overlay --scope=swarm extranet
>*The extranet creation command will ensure that extranet will be available to the docker swarm.*

---

# Guide for Tagging and Pushing Service Images to Docker Hub

This section outlines the steps for tagging and pushing updated Docker images for the DLS_ASSIGNMENT project services to the `rootedatnight/dls_assigment` repository on Docker Hub. Following these steps ensures that the Docker Swarm can use the latest versions of your services.

## Prerequisites

- Docker installed on your machine.
- An account on Docker Hub and access to the `rootedatnight/dls_assigment` repository. If you don't have access, request it from the repository administrator.
- You must be logged in to Docker Hub on your terminal or command prompt. You can log in using `docker login`. 
> If you have Docker desktop, logging in with your terminal wouldn't be necessary.

## Step 1: Tagging the Image

After making changes to your service and testing it locally, you'll need to build and tag the updated Docker image. Tagging correctly identifies the image with the Docker Hub repository and a unique tag (usually the version of the service).

1. Build your Docker image (if you haven't already) with:
   ```bash
   docker build -t <local-image-name>:<tag> .
   ```
   Replace `<local-image-name>` with a name for your image locally and `<tag>` with your new version tag or identifier.

2. Tag the built image for the Docker Hub repository with:
   ```bash
   docker tag <local-image-name>:<tag> rootedatnight/dls_assigment:<service-name>-<version>
   ```
   - `<local-image-name>:<tag>` is the name and tag of your local image.
   - `<service-name>` should be replaced with the name of the service you're updating.
   - `<version>` should be replaced with the new version of your service.

Example:
```bash
docker tag myapi-service:1.0.1 rootedatnight/dls_assigment:api-service-1.0.1
```

## Step 2: Pushing the Image to Docker Hub

Once your image is tagged correctly, you can push it to Docker Hub.

1. Push the image with:
   ```bash
   docker push rootedatnight/dls_assigment:<service-name>-<version>
   ```
   Replace `<service-name>-<version>` with the tag you used in the previous step.

Example:
```bash
docker push rootedatnight/dls_assigment:api-service-1.0.1
```

## Step 3: Update Docker Compose or Swarm Configuration

After pushing the image, make sure to update your `docker-compose.yml` or the Docker Swarm service to use the new image tag. This ensures that the latest version of your service is deployed when the stack is updated or redeployed.

Example `docker-compose.yml` update:
```yaml
services:
  api-service:
    image: rootedatnight/dls_assigment:api-service-1.0.1
    ...
```

## Conclusion

Following these steps whenever you update a service ensures that your changes are correctly versioned and deployed across all environments using Docker Swarm. Always communicate with your team to ensure everyone is aware of new versions and updates.

---

# Docker Swarm Setup Guide for Project DLS_Assignment

## Introduction

This guide outlines the steps required to set up Docker Swarm for the DLS_ASSIGNMENT project, ensuring a consistent development environment across the team. Docker Swarm is used to manage, deploy, and scale the application in a cluster of Docker engines.

## Prerequisites

- Docker installed on your machine (version 19.03 or later recommended)
- Basic understanding of Docker concepts (images, containers, etc.)
- Access to the project's Docker images, either on Docker Hub or a private registry

## Step 1: Initialize Docker Swarm

1. Open a terminal or command prompt.
2. Choose one of the machines to act as the Swarm Manager.
3. Run the following command to initialize the swarm:

   ```bash
   docker swarm init --advertise-addr <MANAGER-IP>
   ```

   Replace `<MANAGER-IP>` with the IP address of the manager node.
   
   Alternatively, if you don't want to specify the MANAGER-IP, you could just run:

   ```bash
   docker swarm init
   ```

## Step 2: Join Swarm Nodes (Optional)

If you have additional machines that you want to act as workers or managers, you need to join them to the swarm:

1. On the manager node, run the following command to generate a join token:

   For worker nodes:

   ```bash
   docker swarm join-token worker
   ```

   For additional manager nodes:

   ```bash
   docker swarm join-token manager
   ```

2. Run the displayed command on each node you wish to join to the swarm.

## Step 3: Create Required Networks (If Applicable)

If your `docker-compose.yml` references any external networks, create them:

```bash
docker network create --driver=overlay --scope=swarm <NETWORK-NAME>
```

## Step 4: Deploy the Stack

1. Clone the repository to your local machine or the swarm manager node.
2. Navigate to the project directory containing the `docker-compose.yml` file.
3. Deploy the stack with:

   ```bash
   docker stack deploy -c docker-compose.yml DLS_ASSIGNMENT_SWARM
   ```

## Step 5: Managing the Swarm

- **View services**: `docker stack services DLS_ASSIGNMENT_SWARM`
- **Scale services**: `docker service scale <SERVICE-NAME>=<NUM-REPLICAS>`
> To automatically have a set number of replicas for a service, update the docker-compose file like so:
```yaml
services:
    api-service:
        ...
        deploy:
            replicas: <NUM-REPLICAS>
```
- **View logs**: `docker service logs <SERVICE-NAME>`

## Step 6: Updating Services

To update a service with a new image:

1. Push the updated image to the Docker registry.
2. Run the following command to update the service:

   ```bash
   docker service update --image <NEW-IMAGE-NAME>:<TAG> <SERVICE-NAME>
   ```

## Removing the Stack

To remove the deployed stack from the swarm:

```bash
docker stack rm DLS_ASSIGMENT_SWARM
```

## Conclusion

This guide covers the basic setup and management of Docker Swarm for the DLS_ASSIGNMENT project. For more advanced configurations and troubleshooting, refer to the [Docker documentation](https://docs.docker.com/engine/swarm/).

---