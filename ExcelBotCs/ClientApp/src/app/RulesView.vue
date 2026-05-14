<script lang="ts" setup>
</script>

<template>
  <aside>
    <h2 class="heading">FC Guidelines</h2>

    <div class="rule">
      <span class="rule-num">1</span>
      <p><strong>Don't be a dick</strong></p>
    </div>

    <div class="rule">
      <span class="rule-num">2</span>
      <p><strong>Honour system of the FC chest:</strong></p>
    </div>

    <p class="intro">
      Here is what is available in the FC chest to <strong>all members!</strong>
    </p>

    <section class="slot">
      <h3 class="slot-title">Slot 1: Collectables Exchange</h3>
      <p>
        This is where any collectables such as minions and orchestrion rolls have been
        deposited. All members have full access to this slot and you are welcome to take anything that you have not
        yet collected.
      </p>
      <p>
        Please don't take items to sell on the marketboard, it is just to share any
        collectables.
      </p>
      <p>
        Naturally we would appreciate any donations if you have any collectables that you
        already have and would like to pass it on.
      </p>
    </section>

    <section class="slot">
      <h3 class="slot-title">Slot 4: FC Pantry</h3>
      <p>
        This slot is for food and pots. Please take as and when you need and not in huge
        bulks (basically ensure that there's enough stuff in there for everyone).
      </p>
      <p>
        1) Old food and pots, including crafting/gathering food. Feel free to take as needed
        if you just need something to help with levelling, etc.
      </p>
      <p>
        2) Current BiS raid food intended for Excel Academy. Please only take 6 prior to
        Excel Academy raid nights. This is paid for by the FC and is intended to help support anyone who raids with
        the FC.
      </p>
    </section>

    <section class="slot">
      <h3 class="slot-title">Slot 5: Dyes & Misc</h3>
      <p>
        If you're looking for a particular dye, we have a huge amount available free to take
        in Slot 5. Feel free to deposit any as well. We also have magicked prisms if you want. No real restriction to
        these.
      </p>
    </section>
  </aside>
</template>

<style scoped>
.heading {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
  margin-bottom: 1.25rem;
  letter-spacing: -0.01em;
  line-height: 1.4;
}

.rule {
  display: flex;
  align-items: baseline;
  gap: 0.625rem;
  margin-bottom: 0.5rem;
}

.rule p {
  margin: 0;
  color: var(--fg);
  line-height: 1.5;
}

.rule-num {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.375rem;
  height: 1.375rem;
  border-radius: 50%;
  font-size: 0.75rem;
  font-weight: 700;
  background: color-mix(in oklab, var(--link) 15%, var(--bg) 85%);
  color: var(--link);
  flex-shrink: 0;
}

.intro {
  margin: 1.25rem 0 1rem;
  color: var(--fg);
  line-height: 1.6;
}

.slot {
  padding-top: 1rem;
  border-top: 1px solid var(--border);
  margin-top: 1rem;
}

.slot-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
  margin-bottom: 0.5rem;
  line-height: 1.4;
}

.slot p {
  margin: 0 0 0.5rem;
  color: var(--fg);
  opacity: 0.85;
  line-height: 1.6;
  font-size: 0.9rem;
}

.slot p:last-child {
  margin-bottom: 0;
}
</style>
