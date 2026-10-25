# Code signing policy

## Status

No production code-signing service has been enabled. No release is currently
signed by SignPath Foundation. The project has not yet submitted its application.
The build workflow intentionally produces unsigned artifacts only.

The planned service, subject to approval, is:
“Free code signing provided by SignPath.io, certificate by SignPath Foundation”.
This describes the intended setup, not current sponsorship or approval.

## Roles and controls

Project maintainer, source contributor, external-contribution reviewer and planned
release approver: [choczajmichal](https://github.com/choczajmichal).
The application and documentation were created with AI assistance under the
maintainer's direction. AI tools are not release approvers.
All team members must enable MFA on GitHub and SignPath.
Every signing request must be manually approved by the designated release approver.
Changes to source code, workflows and build scripts must be reviewed under the
project's contribution policy. The maintainer reviews external pull requests.

Only project-owned binaries built from the public source and verified CI build
may be submitted for signing. Third-party .NET libraries must retain their
original publisher identity; they must not be re-signed as Timerki.
Product metadata must identify Timerki and use a consistent release version.
After signing, verify Authenticode and test every feature with Smart App Control
enabled before describing an artifact as signed or compatible.

## Privacy policy

This program will not transfer any information to other networked systems unless
specifically requested by the user or the person installing or operating it.
Timerki itself implements no network communication. Keyboard events are used
in memory for shortcuts, not retained as typing history or transmitted.
Settings and imported WAV files remain in the local portable directory.
Windows SAPI and the .NET runtime are system/runtime components governed by
their own licenses and Windows privacy settings.

## References

- https://signpath.org/terms.html
- https://signpath.org/apply
- https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control
