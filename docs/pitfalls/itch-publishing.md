# Pitfalls — Publishing (itch.io)


**⚠ The "Save" button clicked by element reference does not save.** [inherited] The page simply
scrolls back to the top, with no error and no banner, and the public page keeps the old text. Wait for
the `.global_flash` "Saved" banner to appear — it is the only sign that tells a submission from a
scroll.

**⚠ The public page is served from a cache.** [inherited] Re-reading it right after a *successful*
save shows it unchanged. Any URL parameter (`?v=2`) is enough to settle it; without one you conclude
there was a failure that never happened, and re-edit for nothing.

**⚠ itch's text editor is a Redactor.** [inherited] The content lives in `.redactor-layer`
(contenteditable), backed by a hidden `textarea`. Writing into the layer does not always synchronise
the textarea — **on the devlog form, never**. A devlog submitted without writing both goes out with a
correct title and an **empty body**.

**⚠ An itch `<select>` has only one option in the DOM**: they are Selectize widgets. Go through
`element.selectize.setValue(...)`, never through a click — which opens a native menu and **freezes
screenshots**.

**⚠ A devlog not ticked "Published" stays a draft without saying so.**

**⚠ Three decisive page settings are in NO file of the repository** [inherited] and therefore never
show when re-reading the code: the **Mobile friendly** checkbox (it alone decides what itch offers a
visitor on a phone), the **Classification** tab (including the player count), and the declared
**orientation**. All three were wrong up to version 1.1.0 of Smily Volley.

**⚠ The itch.io iframe is cross-origin** (`html-classic.itch.zone`): neither injected clicks nor keys
get in. To exercise the **published** build, open the iframe's URL directly in a tab.
