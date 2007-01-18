if not exist Output mkdir Output
if not exist Output\html mkdir Output\html
if not exist Output\art mkdir Output\art
if not exist Output\scripts mkdir Output\scripts
if not exist Output\styles mkdir Output\styles
copy ..\..\Presentation\art\* Output\art
copy ..\..\Presentation\scripts\* Output\scripts
copy ..\..\Presentation\styles\* Output\styles
