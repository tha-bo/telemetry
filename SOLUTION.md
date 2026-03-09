<img width="737" height="338" alt="image" src="https://github.com/user-attachments/assets/02b0ca9a-50f3-4dda-96ca-edf4ce2fe461" />


## Q1. How you would run this in Azure or AWS (key services and how components fit together)

A cloud-based solution can take advantage of cloud resources to scale out and increase reliability. The diagram shows we can separate the app into:

- **Device listener**: Handles incoming events from devices and pushes them to an event hub.
- **Event handler**: Processes events from the hub and records them in the database.
- **API**: Serves frontend requests and exposes telemetry/query endpoints.

## Q2. How you would isolate multiple customers’ data and requests

We can partition database data by customer/client Id. Once we have authentication we can easily add a head to every request to identify the user and their devices

## Q3. How you would handle higher event volumes and protect the system during bursts

The diagram show we can scale each application independently depending on the choke point. Likely the Event handlers will experience the most work and will be allowed to scale out by default. Since events can be out of order and duplicate there is no down side, however the system will be limited by the database throughput

## Q3. A simple CI/CD approach from commit to deployment with automated checks you would add

- We can add tests and linting to the project
- We can use Pipelines to both build and deploy the project. We can also use github actions to do so
- Running linting and tests will be part of the build

## Q4. How you would evolve the data model and deploy changes safely over time

- Use scripts to migrate data
- Version application to help handle any future breaking changes
- Use Azure deployment slots to allow quick deploys with seconds at most downtime (the same for rollbacks)
